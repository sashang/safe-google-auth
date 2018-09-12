open Giraffe
open Giraffe.Serialization
open Microsoft.Extensions.DependencyInjection
open Saturn
open System
open System.IO
open System.Security.Claims
open Microsoft.AspNetCore.Cors.Infrastructure

let publicPath = Path.GetFullPath "../Client/public"
let port = 8085us

type MaybeBuilder() =
    member __.Bind(x, f) =
        match x with
        | Some(x) -> f(x)
        | _ -> None
    member __.Return(x) =
        Some x
    member __.Zero(x) =
        Some x

let maybe = MaybeBuilder()

type UserCredentialsResponse = { user_name : string }

let print_user_details : HttpHandler =
    fun next ctx ->
        ctx.User.Claims |> Seq.iter (fun claim ->
            if claim.Issuer = "Google" && (claim.Type = ClaimTypes.Name || claim.Type = ClaimTypes.Email) then
                printfn "%s" claim.Value)
        next ctx

let login = pipeline {
    requires_authentication (Giraffe.Auth.challenge "Google")
    plug print_user_details
}

let logout = pipeline {
    sign_off "Cookies"
}

let logged_in_view = router {
    pipe_through login

    get "/google" (fun next ctx -> task {
        let name = ctx.User.Claims |> Seq.filter (fun claim -> claim.Type = ClaimTypes.Name) |> Seq.head
        return! json { user_name = name.Value } next ctx
    })
}

let fake_auth_view = router {
    get "/fake-login" (fun next ctx -> task {
        return! json {user_name = "John Doe" } next ctx
    })
}

let webApp = router {
    pipe_through (pipeline { set_header "x-pipeline-type" "Api"
                             set_header "Access-Control-Allow-Origin" "true"})
    forward "/auth" logged_in_view
    forward "/fake-auth" fake_auth_view
}

let configureSerialization (services:IServiceCollection) =
    let fableJsonSettings = Newtonsoft.Json.JsonSerializerSettings()
    fableJsonSettings.Converters.Add(Fable.JsonConverter())
    services.AddSingleton<IJsonSerializer>(NewtonsoftJsonSerializer fableJsonSettings)

let configure_cors (builder : CorsPolicyBuilder) =
    builder.WithOrigins("http://localhost:8085")
        .AllowAnyMethod()
        .AllowAnyHeader()
    |> ignore

let get_env_var var =
    match Environment.GetEnvironmentVariable(var) with
    | null -> None
    | value -> Some value

let app google_id google_secret = 
    application {
        url ("http://0.0.0.0:" + port.ToString() + "/")
        use_router webApp
        memory_cache
        use_static publicPath
        service_config configureSerialization
        use_gzip
        use_google_oauth google_id google_secret "/oauth_callback_google" []
        use_cors "localhost:8080" configure_cors        
    }

(* Use a maybe computation expression. In the case where one is not defined
it will return None, and pass that None value through to the subsequent
expression. It's basically a nested if then else sequence. See
http://www.zenskg.net/wordpress/?p=187 for an example of how this works in
OCaml. In F# you get more syntactically sugar so you don't have to explicitly
write 'bind' everywhere. For more F# specific implementation details see
https://fsharpforfunandprofit.com/posts/computation-expressions-builder-part1/*)
maybe {
    let! google_id = get_env_var "GOOGLE_ID"
    let! google_secret = get_env_var "GOOGLE_SECRET"
    do run (app google_id google_secret)
} |> ignore

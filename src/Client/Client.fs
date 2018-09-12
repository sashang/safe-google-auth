module Client

open Elmish
open Elmish.React
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack
open Fable.Import
open Fulma
open Shared

type LoginState =
| Out
| In

type Msg =
| GetLoginGoogle
| GetFakeLogin
| GotLoginGoogle of UserCredentialsResponse
| GotFakeLogin of UserCredentialsResponse
| ErrorMsg of exn

type Model = {
    login_state : LoginState
    user_info : UserCredentialsResponse option
}

let init _ =
    {login_state = Out; user_info = None}, Cmd.none

let get_credentials () =
    promise {
        let! credentials = Fetch.fetchAs<UserCredentialsResponse> ("/auth/google") []
        return credentials
    }

let get_fake_credentials () =
    promise {
        let! credentials = Fetch.fetchAs<UserCredentialsResponse> ("/fake-auth/fake-login") []
        return credentials
    }

let update (msg : Msg) (model : Model) : Model*Cmd<Msg> =
    match msg with
    | GetLoginGoogle ->
        { login_state = Out; user_info = None }, Cmd.ofPromise get_credentials () GotLoginGoogle ErrorMsg
    | GetFakeLogin ->
        { login_state = Out; user_info = None }, Cmd.ofPromise get_fake_credentials () GotFakeLogin ErrorMsg        
    | GotLoginGoogle credentials ->
        { login_state = In; user_info = Some credentials }, Cmd.none
    | GotFakeLogin credentials ->
        { login_state = In; user_info = Some credentials }, Cmd.none
    | ErrorMsg _ -> { login_state = Out; user_info = None }, Cmd.none


let column (dispatch : Msg -> unit) (model : Model) =
    Column.column [
        Column.Width (Screen.All, Column.Is4)
        Column.Offset (Screen.All, Column.Is4)
    ] [
        Heading.h3 [
            Heading.Modifiers [ Modifier.TextColor IsGrey ]
        ] [ str "Login" ]
        Box.box' [ ] [
            Button.button [
                Button.Color IsPrimary
                Button.IsFullWidth
                Button.OnClick (fun _ -> (dispatch GetLoginGoogle))
                Button.CustomClass "is-large is-block"
            ] [ str "Auth with Google" ]
            Button.button [
                Button.Color IsPrimary
                Button.IsFullWidth
                Button.OnClick (fun _ -> (dispatch GetFakeLogin))
                Button.CustomClass "is-large is-block"
            ] [ str "Fake auth" ]            
        ]
        Text.p [
            Modifiers [ Modifier.TextColor IsGrey ]
        ] [ a [ ] [ str "Current user" ]
            str "\u00A0·\u00A0"
            a [ ] [ str (match model.user_info with
                          | None -> "Unknown user"
                          | Some user -> user.user_name) ]
        ]
    ]



let view (model : Model) (dispatch : Msg -> unit) =
    Hero.hero
        [ Hero.Color IsSuccess
          Hero.IsFullHeight
          Hero.Color IsWhite ]
        [ Hero.body [ ]
            [ Container.container
                [ Container.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                [ column dispatch model ] ] ]


#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update view
#if DEBUG
|> Program.withConsoleTrace
|> Program.withHMR
#endif
|> Program.withReact "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run

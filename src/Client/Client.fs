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
| FirstTime
| Failed

type Msg =
| GetLoginGoogle
| GotLoginGoogle of UserCredentialsResponse
| ErrorMsg of exn

type Model = {
    login_state : LoginState
    user_info : UserCredentialsResponse option
}

let init _ =
    {login_state = FirstTime; user_info = None}, Cmd.none

let get_credentials () =
    promise {
        let! credentials = Fetch.fetchAs<UserCredentialsResponse> ("/api/user-credentials") []
        return credentials
    }

let update (msg : Msg) (model : Model) : Model*Cmd<Msg> =
    match msg with
    | GetLoginGoogle ->
        { login_state = FirstTime; user_info = None }, Cmd.ofPromise get_credentials () GotLoginGoogle ErrorMsg
    | GotLoginGoogle credentials ->
        { login_state = FirstTime; user_info = Some credentials }, Cmd.none
    | ErrorMsg _ -> { login_state = Failed; user_info = None }, Cmd.none


let column (dispatch : Msg -> unit) =
    Column.column
        [ Column.Width (Screen.All, Column.Is4)
          Column.Offset (Screen.All, Column.Is4) ]
        [ Heading.h3
            [ Heading.Modifiers [ Modifier.TextColor IsGrey ] ]
            [ str "Login" ]
          Heading.p
            [ Heading.Modifiers [ Modifier.TextColor IsGrey ] ]
            [ str "Please login to proceed." ]
          Box.box' [ ]
            [ figure [ Class "avatar" ]
                [ img [ Src "https://placehold.it/128x128" ] ]
              form [ ]
                [ Field.div [ ]
                    [ Control.div [ ]
                        [ Input.email
                            [ Input.Size IsLarge
                              Input.Placeholder "Your Email"
                              Input.Props [ AutoFocus true ] ] ] ]
                  Field.div [ ]
                    [ Control.div [ ]
                        [ Input.password
                            [ Input.Size IsLarge
                              Input.Placeholder "Your Password" ] ] ]
                  Field.div [ ]
                    [ Checkbox.checkbox [ ]
                        [ input [ Type "checkbox" ]
                          str "Remember me" ] ]
                  Button.button
                    [ Button.Color IsPrimary
                      Button.IsFullWidth
                      Button.OnClick (fun _ -> (dispatch GetLoginGoogle))
                      Button.CustomClass "is-large is-block" ]
                    [ str "Login" ] ] ]
          Text.p [ Modifiers [ Modifier.TextColor IsGrey ] ]
            [ a [ ] [ str "Sign Up" ]
              str "\u00A0·\u00A0"
              a [ ] [ str "Forgot Password" ]
              str "\u00A0·\u00A0"
              a [ ] [ str "Need Help?" ] ]
          br [ ] ]


let view (model : Model) (dispatch : Msg -> unit) =
    Hero.hero
        [ Hero.Color IsSuccess
          Hero.IsFullHeight
          Hero.Color IsWhite ]
        [ Hero.body [ ]
            [ Container.container
                [ Container.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                [ column dispatch ] ] ]


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

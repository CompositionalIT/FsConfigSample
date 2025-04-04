module Server

open SAFE
open Saturn
open Shared
open FsConfig

[<Convention("APP_NAME")>]
type Config = {
    TodoPrefix: string
    TodoSuffix: string
}

module Storage =
    let todos =
        ResizeArray [
            Todo.create "Create new SAFE project"
            Todo.create "Write your app"
            Todo.create "Ship it!!!"
        ]

    let addTodo todo =
        if Todo.isValid todo.Description then
            todos.Add todo
            Ok()
        else
            Error "Invalid todo"

let todosApi config ctx = {
    getTodos =
        fun () -> async {
            return
                Storage.todos
                |> Seq.map (fun t -> {
                    t with
                        Description = $"%s{config.TodoPrefix}%s{t.Description}%s{config.TodoSuffix}"
                })
                |> List.ofSeq
        }
    addTodo =
        fun todo -> async {
            return
                match Storage.addTodo todo with
                | Ok() -> Storage.todos |> List.ofSeq
                | Error e -> failwith e
        }
}

let webApp config = Api.make (todosApi config)

let app config = application {
    use_router (webApp config)
    memory_cache
    use_static "public"
    use_gzip
}

[<EntryPoint>]
let main _ =
    let config =
        match EnvConfig.Get<Config>() with
        | Ok config -> config
        | Error error ->
            match error with
            | NotFound envVarName -> failwithf "Environment variable %s not found" envVarName
            | BadValue(envVarName, value) -> failwithf "Environment variable %s has invalid value %s" envVarName value
            | NotSupported msg -> failwith msg

    run (app config)
    0
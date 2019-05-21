# dotnet-lambda-exec

A .NET global tool whose purpose is to locally execute AWS Lambda functions.

**Note**: this works only with functions based on the `dotnetcore2.0` runtime,
which uses .NET Core 2.1.

## How it works?
This tool uses 
[EMG.Lambda.LocalRunner](https://github.com/emgdev/lambda-local-runner) under
the covers to run your function.

It does so by parsing your function's `aws-lambda-tools-defaults.json` file
and building an appropriate LocalRunner using Reflection.

## How to use it?

You can install the tool using the dotnet CLI:

> `dotnet tool install -g Musement.LambdaExec`

To invoke it:

> `dotnet lambda-exec --defaults-json={path to JSON}`

By defaults it will look for your function's assembly into the appropriate
directory for the configuration specified in your JSON, but you can force it to
load the Debug build by passing `--debug-mode`.

The default port is 5000, but you can make it use a different one passing
`--port={your port}`.

It can also be used with Visual Studio Code's `launch.json` for an
_F5 Experience_, i.e.

```json
{
    "name": "MyLambda",
    "type": "coreclr",
    "request": "launch",
    "preLaunchTask": "build",
    "program": "dotnet-lambda-exec",
    "env": {
        "FOO": "BAR"
    },
    "args":[
        "--debug-mode",
        "--defaults-json",
        "${workspaceFolder}/src/Functions/MyLambda/aws-lambda-tools-defaults.slots.json"
    ],
    "cwd": "${workspaceFolder}",
    "console": "internalConsole",
    "stopAtEntry": false,
    "internalConsoleOptions": "openOnSessionStart"
}
```

_Debugging your function also works!_

### Environment variables

You can pass environment variables to the function using the standard methods
(on the command line or in the appropriate field in `launch.json` if you're 
using it), but in certain cases you may not want to commit them to source
control (like if you use env vars to pass secrets/keys to the function).
In this case you can use the `--envs-file` parameter to pass the path to a JSON
file structured as a string-string dictionary, i.e.:

```json
{
    "SECRET_KEY": "SuperSecretKey",
    "EXEC_ENV": "Lambda"
}
```

and they will be injected into the process at startup. Keep in mind these will
be added __on top__ of the ones already defined elsewhere, so if you define
`FOO` on your system, on your commandline or in `launch.json` and again in 
`envs.json`, the latter will override the formers.

Keys and values will be passed to `Environment.SetEnvironmentVariable()`
directly, so a `null` value will **unset** the variable.

## What's missing?

The tool is still in its early stage. Here is a list of features planned but
not yet implemented:

- [x] Support for async functions, only synchronous functions are supported at
the moment
- [ ] Custom serializers
- [ ] Smarter search for the assembly. Currently it bails out with an error if
it can't find the assembly at the path it expects given the JSON manifest
- [ ] Ability to pass the path for an assembly directly and find a function
handler autonomously
- [ ] Warnings if execution time and memory limit are exceeded
- [ ] Nicer CLI interface
- [ ] Custom ContentRootPath
- [ ] More realistic `ILambdaContext`
- [ ] Tests :sweat_smile:
- [ ] `.editorconfig`

## How to build

Just `dotnet build`

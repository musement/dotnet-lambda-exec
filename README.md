# dotnet-lambda-exec

A .NET global tool whose purpose is to locally execute AWS Lambda functions.

**Note**: this works only with functions based on the `dotnetcore2.0` runtime.

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

## What's missing?

The tool is still in its early stage. Here is a list of features planned but
not yet implemented:

- [ ] Support for async functions, only synchronous functions are supported at
the moment
- [ ] Custom serializers
- [ ] Smarter search for the assembly. Currently it bails out with an error if
it can't find the assembly at the path it expects given the JSON manifest
- [ ] Ability to pass the path for an assembly directly and find a function
handler autonomously
- [ ] Warnings if execution time and memory limit are exceeded
- [ ] Nicer CLI interface
- [ ] Tests :sweat_smile:
- [ ] `.editorconfig`

## How to build

Just `dotnet build`

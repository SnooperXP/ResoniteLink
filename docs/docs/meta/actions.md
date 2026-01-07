---
title: GitHub Actions
---

The repository for ResoniteLink has several GitHub Actions workflows that are divided in two types.

## Composite Actions

A composite Action is a reusable bit of workflow that you can use as a step.

They are useful in case there are repeated steps over multiple workflows, for instance, publishing ResoniteLink will
need for it to be built, so the composite action can be used to avoid copy-pasting steps.

Composite Actions generally need to inherit the inputs of the other composite Actions used.

### build-link

This Action builds ResoniteLink and outputs the NuGet package to the `nupkgs` directory in `github.workspace`.

#### Used actions

- `actions/setup-dotnet@v5`

#### Inputs

| Name              | Description                                        | Default        | Required |
|-------------------|----------------------------------------------------|----------------|----------|
| `dotnet-version`  | Version of .NET to install for the build process.  | `10.0.x`       | false    |
| `additional-args` | Additional arguments to pass to the build command. |                | false    |
| `out-folder`      | Output folder for the built files.                 | `nupkgs`       | false    |
| `projectdir`      | Directory of the project to build.                 | `ResoniteLink` | false    |

#### Outputs

None.

#### Sample usage

```yaml
  - name: 'Build ResoniteLink'
    uses: ./.github/actions/build-link
    with:
      out-folder: '${{ github.workspace }}/myoutfolder'
```

### publish-link

This Action publishes ResoniteLink to the official NuGet repository.

#### Used actions

- `./.github/actions/build-link`

#### Inputs

| Name              | Description                                        | Default                               | Required |
|-------------------|----------------------------------------------------|---------------------------------------|----------|
| `dotnet-version`  | Version of .NET to install for the build process.  | `10.0.x`                              | false    |
| `additional-args` | Additional arguments to pass to the build command. |                                       | false    |
| `out-folder`      | Output folder for the built files.                 | `nupkgs`                              | false    |
| `projectdir`      | Directory of the project to build.                 | `ResoniteLink`                        | false    |
| `nuget-token`     | Token to authenticate against the NuGet registry.  |                                       | true     |
| `nuget-url`       | URL towards the NuGet registry.                    | `https://api.nuget.org/v3/index.json` | false    |

#### Outputs

None.

#### Sample usage

```yaml
      - name: 'Publish ResoniteLink'
        uses: ./.github/actions/publish-link
        with:
          out-folder: ${{ steps.get-folder.outputs.out-folder }}
          additional-args: '-p:Version=${{ github.ref_name }}'
          nuget-token: ${{ secrets.NUGET_TOKEN }}
```

## Workflows

Workflows are what's executing and building all the steps, and most importantly, using the Actions.

### build-publish

In charge of building and publishing ResoniteLink when needed.

#### Triggers

- When a push is done on any branch
- When a pull request is opened, re-opened or synchronized
- When a release is published

#### Actions used

- `actions/checkout@v6`
- `./.github/actions/build-link`
- `./.github/actions/publish-link`

### docs-generation

In charge of generating, then publishing the documentation you're reading right now.

#### Triggers

- When a release is published
- Manually via `workflow_dispatch`

#### Actions used

- `actions/checkout@v6`
- `actions/setup-dotnet@v5`
- `actions/upload-pages-artifact@v4`
- `actions/deploy-pages@v4`

#### Inputs

If manual:

| Name  | Description                                 | Default  | Required |
|-------|---------------------------------------------|----------|----------|
| `ref` | The branch or tag the workflow will run on. | `master` | false    |

#### Deployment environment

- `github-pages` to https://yellow-dog-man.github.io/ResoniteLink/

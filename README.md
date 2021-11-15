<div align="center">
	<h1>Update</h1>
	<img src="./Sewer56.Update/Merge.png" width="150" align="center" />
	<br/> <br/>
	<strong>An Extensible Update Library</strong>
	<br/>
    For anything, from `CurrentProgram` to Plugins.
</div>

## Documentation

For the latest documentation for Update and more details about the library, please see the dedicated [documentation site](https://sewer56.dev/Update/).
Alternatively, check the `docs` folder of this repository.

## About Update

`Update` is a lightweight-ish updating framework for .NET applications. 

It is designed with the purpose of updating arbitrary things, including but not limited to:  
- Current Application  
- Plugins  
- Modules  

The goal of this library is to be extensible; allowing users to easily add support for their own components such as download sources and compression formats without requiring changes to the library code.

Update is heavily inspired by [Onova](https://github.com/Tyrrrz/Onova) by Alexey Golub and has a somewhat similar API. `Update` in particular adds additional features such as delta compression at the expense of a slightly more complex configuration process.

## When to use Update

- You ship very big updates and require delta compression support between versions.
- You want to clean up your application folder after updates.
- You want to update things other than just the application you are running.
- You need to support Semantic Versioning (and thus Prereleases).

## When to not use Update

Consider using the original [Onova](https://github.com/Tyrrrz/Onova) (or another library) if you have any of the following requirements:

- If you wish to use this library with .NET Framework (VCDiff needs backported).
- You need a simpler CI/CD deployment & integration experience.
- You can only upload 1 file to a given website.

## Etymology

Update is a pun on the name [Onova](https://github.com/Tyrrrz/Onova), which is the Ukrainian word for "update" (noun).

## Icon
[Merge](https://thenounproject.com/search/?q=merge&i=1404538) by Creaticca Creative Agency from the Noun Project
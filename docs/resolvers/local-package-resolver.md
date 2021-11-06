## Local Package Resolver

The `Local Package Resolver` reads releases from a local directory on your machine.

### Example Usage

```csharp
// Use Local Package Resolver
var resolver = new LocalPackageResolver(OutputFolder);
await resolver.InitializeAsync();
var versions = await resolver.GetPackageVersionsAsync();
```
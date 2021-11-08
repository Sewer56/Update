## Aggregate Package Resolver

The `Aggregate Package Resolver` obtains releases from a collection of other, external package resolvers.
The priority of the resolvers is based on their list order from first (highest) to last (lowest) priority.

### Example Usage

```csharp
// Add 2 package resolvers to the aggregate resolver.
var resolver = new AggregatePackageResolver(new List<IPackageResolver>()
{
    new GameBananaUpdateResolver(GameBananaConfig, commonResolverSettings), // Higher Priority
    new GitHubReleaseResolver(GitHubConfig, commonResolverSettings),        // Lower Priority
});

await resolver.InitializeAsync();
var versions = await resolver.GetPackageVersionsAsync();

// etc.
```
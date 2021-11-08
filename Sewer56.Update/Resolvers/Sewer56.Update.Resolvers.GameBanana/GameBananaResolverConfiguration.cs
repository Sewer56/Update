using System;
using System.Collections.Generic;
using System.Text;

namespace Sewer56.Update.Resolvers.GameBanana;

/// <summary>
/// Configuration for the GameBanana item resolver.
/// </summary>
public class GameBananaResolverConfiguration
{
    /// <summary>
    /// Legacy use only. All items today and in the future are going to be "Mod".
    /// </summary>
    public string ModType { get; set; } = "Mod";

    /// <summary>
    /// Unique identifier for the individual mod. This is the last number of a GameBanana Mod Page URL
    /// e.g. https://gamebanana.com/mods/150118 -> 150118
    /// </summary>
    public int ItemId { get; set; }
}
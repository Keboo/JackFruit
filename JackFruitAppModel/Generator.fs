﻿namespace Jackfruit

// KAD-Chet: I am not getting Windows to correctly display the AssemblyVersion

open Microsoft.CodeAnalysis
open Generator.SourceGenerator
open Common
open Jackfruit.Models


[<Generator>]
type Generator() =
    inherit CliSourceGenerator<TreeNodeType<ArchetypeInfo>>()

    override _.GetAppModel synataxTrees semanticModel =
        Jackfruit.AppModel()


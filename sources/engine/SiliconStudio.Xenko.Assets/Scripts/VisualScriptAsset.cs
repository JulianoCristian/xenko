using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using SiliconStudio.Assets;
using SiliconStudio.Assets.Serializers;
using SiliconStudio.Core;
using SiliconStudio.Core.Collections;
using SiliconStudio.Core.IO;

namespace SiliconStudio.Xenko.Assets.Scripts
{
    [AssetPartReference(typeof(Method), typeof(Block), typeof(Link), typeof(Symbol))]
    [AssetPartReference(typeof(Block), typeof(Slot), ReferenceType = typeof(BlockReference), KeepTypeInfo = false)]
    [AssetPartReference(typeof(Link))]
    [AssetPartReference(typeof(Symbol))]
    [AssetPartReference(typeof(Slot))]
    [DataContract("VisualScriptAsset")]
    [Display(85, "Visual Script")]
    [AssetDescription(FileExtension)]
    public class VisualScriptAsset : AssetComposite, IProjectFileGeneratorAsset
    {
        /// <summary>
        /// The default file extension used by the <see cref=VisualScriptAsset"/>.
        /// </summary>
        public const string FileExtension = ".xkvs";

        [DataMember(0)]
        [DefaultValue(Accessibility.Public)]
        public Accessibility Accessibility { get; set; } = Accessibility.Public;

        [DataMember(10)]
        [DefaultValue(false)]
        public bool IsStatic { get; set; }

        [DataMember(20)]
        public string BaseType { get; set; }

        [DataMember(30)]
        public string Namespace { get; set; }

        /// <summary>
        /// The list of using directives.
        /// </summary>
        [DataMember(40)]
        public TrackingCollection<string> UsingDirectives { get; set; } = new TrackingCollection<string>();

        /// <summary>
        /// The list of member variables (properties and fields).
        /// </summary>
        [DataMember(50)]
        public TrackingCollection<Property> Properties { get; } = new TrackingCollection<Property>();

        /// <summary>
        /// The list of functions.
        /// </summary>
        [DataMember(60)]
        public TrackingCollection<Method> Methods { get; } = new TrackingCollection<Method>();

        #region IProjectFileGeneratorAsset implementation

        [DataMember(Mask = DataMemberAttribute.IgnoreMask)]
        [Display(Browsable = false)]
        public string Generator { get; } = "XenkoVisualScriptGenerator";

        #endregion

        /// <inheritdoc/>
        public override IEnumerable<AssetPart> CollectParts()
        {
            foreach (var member in Properties)
                yield return new AssetPart(member.Id, member.Base, newBase => member.Base = newBase);
            foreach (var function in Methods)
            {
                yield return new AssetPart(function.Id, function.Base, newBase => function.Base = newBase);
                foreach (var parmeter in function.Parameters)
                    yield return new AssetPart(parmeter.Id, parmeter.Base, newBase => parmeter.Base = newBase);
                foreach (var block in function.Blocks)
                    yield return new AssetPart(block.Id, block.Base, newBase => block.Base = newBase);
                foreach (var link in function.Links)
                    yield return new AssetPart(link.Id, link.Base, newBase => link.Base = newBase);
            }
        }

        /// <inheritdoc/>
        public override bool ContainsPart(Guid id)
        {
            foreach (var variable in Properties)
            {
                if (variable.Id == id)
                    return true;
            }

            foreach (var method in Methods)
            {
                if (method.Id == id)
                    return true;

                if (method.Blocks.ContainsKey(id) || method.Links.ContainsKey(id))
                    return true;

                foreach (var parameter in method.Parameters)
                {
                    if (parameter.Id == id)
                        return true;
                }

                foreach (var block in method.Blocks)
                {
                    foreach (var slot in block.Slots)
                    {
                        if (slot.Id == id)
                            return true;
                    }
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public override IIdentifiable FindPart(Guid id)
        {
            foreach (var variable in Properties)
            {
                if (variable.Id == id)
                    return variable;
            }

            foreach (var method in Methods)
            {
                if (method.Id == id)
                    return method;

                Block matchingBlock;
                if (method.Blocks.TryGetValue(id, out matchingBlock))
                    return matchingBlock;

                foreach (var parameter in method.Parameters)
                {
                    if (parameter.Id == id)
                        return parameter;
                }

                foreach (var block in method.Blocks)
                {
                    foreach (var slot in block.Slots)
                    {
                        if (slot.Id == id)
                            return slot;
                    }
                }
            }

            return null;
        }

        /// <inheritdoc/>
        protected override object ResolvePartReference(object partReference)
        {
            var propertyReference = partReference as Property;
            if (propertyReference != null)
            {
                foreach (var property in Properties)
                {
                    if (property.Id == propertyReference.Id)
                    {
                        return property;
                    }
                }
                return null;
            }

            var parameterReference = partReference as Parameter;
            if (parameterReference != null)
            {
                foreach (var method in Methods)
                {
                    foreach (var parameter in method.Parameters)
                    {
                        if (parameter.Id == parameterReference.Id)
                        {
                            return method;
                        }
                    }
                }
                return null;
            }

            var methodReference = partReference as Method;
            if (methodReference != null)
            {
                foreach (var method in Methods)
                {
                    if (method.Id == methodReference.Id)
                    {
                        return method;
                    }
                }
                return null;
            }

            var blockReference = partReference as Block;
            if (blockReference != null)
            {
                foreach (var function in Methods)
                {
                    Block realPart;
                    if (function.Blocks.TryGetValue(blockReference.Id, out realPart))
                        return realPart;
                }
                return null;
            }

            var linkReference = partReference as Link;
            if (linkReference != null)
            {
                foreach (var function in Methods)
                {
                    Link realPart;
                    if (function.Links.TryGetValue(linkReference.Id, out realPart))
                        return realPart;
                }
                return null;
            }

            var slotReference = partReference as Slot;
            if (slotReference != null)
            {
                // TODO: store slot reference as Block Id + Slot Id for faster lookup?
                foreach (var function in Methods)
                {
                    foreach (var block in function.Blocks)
                    {
                        foreach (var slot in block.Slots)
                        {
                            if (slot.Id == slotReference.Id)
                                return slot;
                        }
                    }
                }

                return null;
            }

            return null;
        }

        public void SaveGeneratedAsset(AssetItem assetItem)
        {
            var generatedAbsolutePath = assetItem.GetGeneratedAbsolutePath();

            var compilerResult = Compile(assetItem);
            File.WriteAllText(generatedAbsolutePath, compilerResult.GeneratedSource);
        }

        public VisualScriptCompilerResult Compile(AssetItem assetItem)
        {
            var generatedAbsolutePath = assetItem.GetGeneratedAbsolutePath();

            var compilerOptions = new VisualScriptCompilerOptions
            {
                Class = Path.GetFileNameWithoutExtension(generatedAbsolutePath),
            };

            // Try to get root namespace from containing project
            // Since ProjectReference.Location is sometimes absolute sometimes not, we have to handle both case
            // TODO: ideally we should stop converting those and handle this automatically in a custom Yaml serializer?
            var sourceProjectAbsolute = assetItem.SourceProject;
            var sourceProjectRelative = sourceProjectAbsolute?.MakeRelative(assetItem.Package.FullPath.GetFullDirectory());
            var projectReference = assetItem.Package.Profiles.SelectMany(x => x.ProjectReferences).FirstOrDefault(x => x.Location == (x.Location.IsAbsolute ? sourceProjectAbsolute : sourceProjectRelative));

            if (projectReference != null)
            {
                // Find root namespace from project
                var rootNamespace = projectReference?.RootNamespace ?? projectReference.Location.GetFileName();
                if (rootNamespace != null)
                {
                    compilerOptions.DefaultNamespace = rootNamespace;

                    // Complete namespace with "Include" folder (if not empty)
                    var projectInclude = assetItem.GetProjectInclude();
                    if (projectInclude != null)
                    {
                        var lastDirectorySeparator = projectInclude.LastIndexOf('\\');
                        if (lastDirectorySeparator != -1)
                        {
                            var projectIncludeFolder = projectInclude.Substring(0, lastDirectorySeparator);
                            compilerOptions.DefaultNamespace += '.' + projectIncludeFolder.Replace('\\', '.');
                        }
                    }
                }
            }

            var compilerResult = VisualScriptCompiler.Generate(this, compilerOptions);
            return compilerResult;
        }
    }
}
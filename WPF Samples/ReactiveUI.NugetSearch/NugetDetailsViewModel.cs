using System;
using System.Diagnostics;
using System.Reactive;
using NuGet.Protocol.Core.Types;
using ReactiveUI;

namespace ReactiveUI.Samples
{
    // This class wraps out NuGet model object into a ViewModel and allows
    // us to have a ReactiveCommand to open the NuGet package URL.
    public class NugetDetailsViewModel : ReactiveObject
    {
        private readonly IPackageSearchMetadata _metadata;
        private readonly Uri _defaultUrl;

        public NugetDetailsViewModel(IPackageSearchMetadata metadata)
        {
            _metadata = metadata;
            _defaultUrl = new Uri("https://git.io/fAlfh"); 
        }

        public Uri IconUrl => _metadata.IconUrl ?? _defaultUrl;
        public string Description => _metadata.Description;
        public Uri ProjectUrl => _metadata.ProjectUrl;
        public string Title => _metadata.Title; 
         
    }
}

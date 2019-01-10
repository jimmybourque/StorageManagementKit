using StorageManagementKit.Core.Diagnostics;
using StorageManagementKit.Core.Copying.Destinations;
using StorageManagementKit.Core.Transforms;
using System;

namespace StorageManagementKit.Core.Copying.Sources
{
    public class RepositorySourceFactory
    {
        #region Members
        private ILogging _logger;
        private IProgressing _progress;
        #endregion

        #region Constructors
        public RepositorySourceFactory(ILogging logger, IProgressing progress)
        {
            _logger = logger ?? throw new ArgumentNullException("logger");
            _progress = progress ?? throw new ArgumentNullException("progress");
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Create a <see cref="IRepositorySource"/> instance in according to the command arguments
        /// </summary>
        public IRepositorySource Create(SourceSettings srcSettings, DestinationSettings dstSettings, TransformSettings trfSettings)
        {
            srcSettings = srcSettings ?? throw new ArgumentNullException("srcSettings");
            dstSettings = dstSettings ?? throw new ArgumentNullException("dstSettings");
            trfSettings = trfSettings ?? throw new ArgumentNullException("trfSettings");

            IRepositorySource source;

            switch (srcSettings.Repository)
            {
                case SourceRepository.Local:
                    {
                        Helpers.MandatoryValue("source path", srcSettings.Path);

                        source = new LocalDirectorySource(srcSettings.Path, _progress, srcSettings.WideDisplay,
                            srcSettings.CheckLevel, srcSettings.NoCleaning);
                        break;
                    }

                case SourceRepository.GCS:
                    {
                        Helpers.MandatoryValue("source path", srcSettings.Path);
                        Helpers.MandatoryValue("source api key filename", srcSettings.ApiKey);

                        source = new GcsBucketSource(srcSettings.Path, srcSettings.ApiKey, _progress, srcSettings.WideDisplay);
                        break;
                    }

                case SourceRepository.S3:
                    {
                        Helpers.MandatoryValue("source path", srcSettings.Path);
                        Helpers.MandatoryValue("source api key filename", srcSettings.ApiKey);

                        source = new S3BucketSource(srcSettings.Path, srcSettings.ApiKey, _progress, srcSettings.WideDisplay);
                        break;
                    }

                default:
                    throw new SmkException($"Unsupported repository source '{srcSettings.Repository}");
            }

            source.Transform = new TransformFactory(_logger).Create(trfSettings);
            source.Destination = new RepositoryDestinationFactory(_logger, source.Transform).Create(dstSettings);
            source.Logger = _logger;

            return source;
        }
        #endregion
    }
}

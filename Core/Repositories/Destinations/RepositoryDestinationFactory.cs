﻿using StorageManagementKit.Core.Diagnostics;
using StorageManagementKit.Core.Transforms;
using System;

namespace StorageManagementKit.Core.Repositories.Destinations
{
    public class RepositoryDestinationFactory
    {
        #region Members
        private ILogging _logger;
        private ITransforming _transform;
        #endregion

        #region Constructors
        public RepositoryDestinationFactory(ILogging logger, ITransforming transform)
        {
            _logger = logger ?? throw new ArgumentNullException("logger");
            _transform = transform;
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Create a <see cref="IRepositoryDestination"/> instance in according to the command arguments
        /// </summary>
        public IRepositoryDestination Create(DestinationSettings settings)
        {
            settings = settings ?? throw new ArgumentNullException("settings");

            switch (settings.Repository)
            {
                case DestinationRepository.Local:
                    {
                        Helpers.MandatoryValue("destination", settings.Path);

                        LocalDirectoryDestination dest = new LocalDirectoryDestination(settings.Path, true, _transform);
                        dest.Logger = _logger;
                        return dest;
                    }

                case DestinationRepository.GCS:
                    {
                        Helpers.MandatoryValue("destination", settings.Path);
                        Helpers.MandatoryValue("OAuth filename", settings.OAuthFile);

                        GcsBucketDestination dest = new GcsBucketDestination(settings.Path, settings.OAuthFile);
                        dest.Logger = _logger;
                        return dest;
                    }

                default:
                    throw new JboBackupException($"Unsupported repository destination '{settings.Repository}");
            }
        }
        #endregion
    }
}
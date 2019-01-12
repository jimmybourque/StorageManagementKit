using StorageManagementKit.Diagnostics.Logging;
using StorageManagementKit.Security.Crypto;
using StorageManagementKit.Types;
using System;

namespace StorageManagementKit.Core.Transforms
{
    public class TransformFactory
    {
        private ILogging _logger;

        /// <summary>
        /// Constructor
        /// </summary>
        public TransformFactory(ILogging logger)
        {
            _logger = logger ?? throw new ArgumentNullException("logger");
        }

        /// <summary>
        /// Create a <see cref="ITransforming"/> instance in according to the command arguments
        /// </summary>
        public ITransforming Create(TransformSettings settings)
        {
            settings = settings ?? throw new ArgumentNullException("settings");

            byte[] key, iv;

            // Creates the transformation instance
            switch (settings.Kind)
            {
                case TransformKind.Secure:
                    {
                        Helpers.MandatoryValue("key filename", settings.TripleDesFilename);

                        if (!TripleDES.LoadKeyFile(settings.TripleDesFilename, out key, out iv, _logger))
                            throw new SmkException(ErrorResources.TransformFactory_InstanciationFailed);

                        SecureTransform trans = new SecureTransform(key, iv, _logger);
                        trans.Logger = _logger;
                        return trans;
                    }

                case TransformKind.Unsecure:
                    {
                        Helpers.MandatoryValue("key filename", settings.TripleDesFilename);

                        if (!TripleDES.LoadKeyFile(settings.TripleDesFilename, out key, out iv, _logger))
                            return null;

                        UnsecureTransform dest = new UnsecureTransform(key, iv, _logger);
                        dest.Logger = _logger;
                        return dest;
                    }

                case TransformKind.None:
                    {
                        return null;
                    }

                default:
                    throw new SmkException($"Unsupported transformation kind '{settings.Kind}");
            }
        }
    }
}

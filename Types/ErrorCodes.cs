﻿namespace StorageManagementKit.Types
{
    public static class ErrorCodes
    {
        public const int Engine_FileEncryption = 100;
        public const int Engine_FileDecryption = 101;

        public const int SyncPhase_SendingBegun = 200;
        public const int SyncPhase_SendingEnded = 201;
        public const int DirectorySync_ScanInterrupted = 202;
        public const int Engine_OperationFailed_Busy = 203;
        public const int Engine_BackupException = 204;
        public const int Engine_CancellationDetected = 206;
        public const int LocalDirectoryDestination_CommitException = 207;
        public const int LocalDirectorySource_FileFoundException = 208;
        public const int LocalDirectorySource_FileProcessing = 209;
        public const int UnsecureTransform_InvalidMetadataSignature = 210;
        public const int UnsecureTransform_InvalidDataContentSignature = 211;
        public const int LocalDirectorySource_IgnoredFile = 212;
        public const int LocalDirectorySource_SyncFile = 213;
        public const int TripleDES_GenerateKey = 214;
        public const int TripleDES_KeyfileNotFound = 215;
        public const int TripleDES_InvalidKey = 216;
        public const int LocalDirectorySource_InvalidMD5File = 217;
        public const int LocalDirectoryDestination_DeletionFailed = 218;
        public const int LocalDirectoryDestination_FileDeleted = 219;
        public const int SyncPhase_DeletionBegun = 220;
        public const int SyncPhase_DeletionEnded = 221;
        public const int GcsBucketDestination_UnsecuredNotSupported = 222;
        public const int GcsBucketDestination_CommitException = 223;
        public const int GcsBucketDestination_GettingListException = 224;
        public const int GcsBucketDestination_FileDeleted = 235;
        public const int SyncPhase_DeletionBegun2 = 236;
        public const int SyncPhase_DeletionEnded2 = 237;
        public const int GcsBucketSource_MissingMetadata = 238;
        public const int SyncPhase_SendingBegun2 = 239;
        public const int SyncPhase_SendingEnded2 = 240;
        public const int GcsBucketSource_IgnoredFile = 241;
        public const int GcsBucketDestination_GettingObjectList = 242;
        public const int GcsBucketSource_GettingObjectList = 243;
        public const int GcsBucketSource_SyncFile = 244;
        public const int DirectoryDiscover_GettingFileList = 245;
        public const int LocalDirectoryDestination_DirectoryDeleted = 246;
        public const int LocalDirectoryDestination_DirectoryDeletionException = 247;
        public const int LocalDirectoryCleaner_FailedToDeleteFile = 248;
        public const int LocalDirectoryCleaner_DeletedFile = 249;
        public const int SyncPhase_CleaningBegun = 250;
        public const int SyncPhase_CleaningEnded = 251;
        public const int GcsObjectRestore_GetVersionsException = 252;
        public const int GcsObjectRestore_RestoreObjectException = 253;
        public const int GcsObjectRestore_MissingMetadata = 254;
        public const int GcsObjectRestore_ObjectNotFound = 255;
        public const int S3BucketDestination_GettingListException = 256;
        public const int S3BucketDestination_UnsecuredNotSupported = 257;
        public const int S3BucketDestination_CommitException = 258;
        public const int S3BucketDestination_GettingObjectList = 259;
        public const int S3BucketDestination_FileDeleted = 260;
        public const int SyncPhase_DeletionBegun3 = 261;
        public const int SyncPhase_DeletionEnded3 = 262;
        public const int SyncPhase_SendingBegun3 = 263;
        public const int SyncPhase_SendingEnded3 = 264;
        public const int S3BucketSource_GettingObjectList = 265;
        public const int S3BucketSource_SyncFile = 266;
        public const int S3BucketSource_IgnoredFile = 267;
        public const int S3BucketSource_MissingMetadata = 268;
        public const int S3ObjectRestore_ObjectNotFound = 269;
        public const int S3ObjectRestore_GetVersionsException = 270;
        public const int S3ObjectRestore_RestoreObjectException = 271;
        public const int S3ObjectRestore_MissingMetadata = 272;
    }
}
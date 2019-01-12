## Storage Management Kit

The storage software kit allows you to move the content between many file storage. It can recover specific file from the an encrypted backup. The kit is designed to ensure a secure way to manage your backup. You keep the encryption key on your side; never in the cloud. Your files are never sent to a cloud service without being encrypted.

The kit integrates cloud services of _Google Cloud Bucket Storage (GCS)_ and _Amazon Bucket Storage (S3)_. The organic architecture allows you to add a non supported yet of source or destination.

To compile the source code, you must install .Net Core 2.1

## Architecture

![GCP](https://github.com/jimmybourque/StorageManagementKit/blob/master/Doc/Images/CloudServiceLogo-GCP.png)
![S3](https://github.com/jimmybourque/StorageManagementKit/blob/master/Doc/Images/CloudServiceLogo-S3.png)

The "Copy" utility has a flexible architecture :

1. You have a repository like as a folder or a bucket located in a cloud storage
1. The source is now identified
1. Determines if you want to encrypt or decrypt objects into the source
1. You have a repository located in a folder or a cloud storage


![Flow local to GCS](https://github.com/jimmybourque/StorageManagementKit/blob/master/Doc/Images/SmkCopyOrganicArchitecture.png) 

Refers to the [WIKI](https://github.com/jimmybourque/StorageManagementKit/wiki) for get more information.

## Storage Management Kit

The storage software kit is a multi-cloud integration solution that allows you to move the content between many storage files. It can recover specific files from an encrypted backup. The kit is designed to ensure a secure way to manage your backup. You keep the encryption key by your side; never in the cloud. Your files are never sent to a cloud service without being encrypted.

The kit integrates the cloud services of _Google Cloud Bucket Storage (GCS)_ and _Amazon Bucket Storage (S3)_. The organic architecture allows you to add a non-supported source or destination.

"_This is my personal backup solution that I use at home_" - Jimmy Bourque

To compile the source code, you must install .Net Core 2.1

## Integrations

![GCP](https://github.com/jimmybourque/StorageManagementKit/blob/master/Doc/Images/CloudServicesLogo.png)

## Architecture

The "Copy" utility has a flexible architecture :

1. You have a repository such as a folder or a bucket located in a cloud storage
1. The source is now identified
1. Determines if you want to encrypt or decrypt objects into the source
1. You have a repository located in a folder or a cloud storage


![Flow local to GCS](https://github.com/jimmybourque/StorageManagementKit/blob/master/Doc/Images/SmkCopyOrganicArchitecture.png) 

Refers to the [WIKI](https://github.com/jimmybourque/StorageManagementKit/wiki) for get more information.

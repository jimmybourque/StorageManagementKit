## Storage Management Kit

The storage software kit allows you to move the content from a storage to another one. Wich this kit, you can backup your files from an on-prem file server or from a personal computer to a cloud based storage solution. All the operations are securely done without storing any non secured file. Files can be securely stored with a client-side encryption.

To compile the source code, you must install .Net Core 2.0

## Architecture

The "Copy" utility has a flexible architecture :

1. You have a repository like as a folder or a bucket located in a cloud storage
1. The source is now identified
1. Determines if you want to encrypt or decrypt objects into the source
1. You have a repository located in a folder or a cloud storage


![Flow local to GCS](https://github.com/jimmybourque/StorageManagementKit/blob/master/Doc/Images/SmkCopyOrganicArchitecture.png) 

Refers to the [WIKI](https://github.com/jimmybourque/StorageManagementKit/wiki) for get more information.

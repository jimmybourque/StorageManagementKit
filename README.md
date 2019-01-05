## Storage Management Kit

The storage software kit allows you to move the content from a storage to another one. Wich this kit, you can backup your files from an on-prem file server or from a personal computer to a cloud based storage solution.


## Architecture

The "Copy" utility has a flexible architecture :

1. You have a repository like as a folder or a bucket located in a cloud storage
1. The source is identified
1. Determines if you want to encrypt of decrypt objects into the source
1. You have a repository located in a folder or a cloud storage


![Flow local to GCS](https://github.com/jimmybourque/StorageManagementKit/blob/master/Doc/Images/OrganicArchitecture.png) 

By example, you can use this kit for a backup strategy solution or a file server migration tool. Otherwise, you could use it to recover a backup from the a cloud storage. By using the command line switches, you can choose the source, the transformation and the destination as you need.
 
 
## Scenario 1 - Using Google Cloud Storage
![Flow local to GCS](https://github.com/jimmybourque/StorageManagementKit/blob/master/Doc/Images/FlowLocalToGCS.png) 
```
> dotnet StorageManagementKit.Copy.dll /src=local /srcpath=E:\ /dst=gcs /dstpath=my-bucket-name /transform=secure /dstoauth=my_auth_json /filekey=my_key.dat
```


## Scenario 2 - Using Google Cloud Storage
![Flow GCS to local](https://github.com/jimmybourque/StorageManagementKit/blob/master/Doc/Images/FlowGCSToLocal.png) 
```
dotnet StorageManagementKit.Copy.dll /src=gcs /srcpath=my-bucket-name /dst=local /dstpath=E:\ /transform=unsecure /dstoauth=my_auth_json /filekey=my_key.dat`
```


## Scenario 3 - Using Local Storage
![Flow local to local](https://github.com/jimmybourque/StorageManagementKit/blob/master/Doc/Images/FlowLocalToLocal.png) 
```
dotnet StorageManagementKit.Copy.dll /src=local /srcpath=D:\ /dst=local /dstpath=E:\ /transform=secure /filekey=my_key.dat`
```


## Scenario 4 - Using Amazon S3
![Flow local to S3](https://github.com/jimmybourque/StorageManagementKit/blob/master/Doc/Images/FlowLocalToS3.png) 
```
dotnet StorageManagementKit.Copy.dll /src=local /srcpath=D:\ /dst=s3 /dstpath=my-bucket-name /transform=secure /dstoauth=my_auth_json /filekey=my_key.dat`
```

ringtail-deploy-service
===================

This project is a collection of tools that live on a Ringtail machine and facilitate installations and upgrades.  It consists of two major parts - a web-service API, and a set of loosely coupled command line utilities to do installations.

##Getting Started

###Building Binaries
The first step is building the binaries.

Build the the solutions: RingtailAutoInstaller.sln and InstallerService.sln

Building these solutions will place the executables and all configuation files in the `bin` folder. 

`bin\InstallerService` contains the Service and WebAPI endpoint

`bin\AutoDeploy` contains the command line utilities used to deploy Ringtail


###Service Installation
Once you have binaries you can install the service on a machine

1.  Create the `C:\Upgrade` folder if it does not already exist
2.  Copy `bin\InstallerService` to `C:\Upgrade\InstallerService`
3.  Copy `bin\AutoDeploy` to `C:\Upgrade\AutoDeploy`
4.  Grant Logon as Service rights to a Local Admin account
5.  Install the Windows Service using your Logon as Service account 
```
sc create RingtailDeployService binPath= "C:\Upgrade\InstallerService\InstallerWindowsService.exe" obj= "domain\username" password= "password" start= "auto"
sc description RingtailDeployService "Helps install Ringtail"
```

Finally, start the `RingtailDeployService`
```
net start RingtailDeployService
```


###Service Configuration
The service uses a number of key-value pair configuration of various application subsets.

#####InstallerService\upgrade.config

This config file uses the format
```
KEY|VALUE
```

*DROP_FOLDER*
The location where the service can self-update itself from. It is tied to the `/api/UpdateInstallerService` API. When configured and the `/api/UpdateInstallerService` is called it will retrieve the binaries at the specified path and overwrite the files currently installed on the machine. 

#####InstallerService\config.config
This config uses the format
```
KEY|VALUE
```
*DeployPath*
The path where the deployment utility is installed. It should be configured to `C:\Upgrade\AutoDeploy` by default.  Changing this value has not been testing.

*MasterRunnerUser*, *MasterRunnerPass*
The user and password that the RingtailDeployService is running under.  In certain circumstances, the C# Process spawning mechanism was losing the Admin security token that the service is running under. This information is explicitly entered in this configuration to ensure that all spawned processes launch with the correct credentials and elevated security token.



#####AutoDeploy\volitleData.config
This configures the variables used by the components needed to install Ringtail. This config uses the format:
```
App|KEY="VALUE"
```

##Contributing

In lieu of a formal style guide, please maintain consistency with style and patterns in place in the application. Add appropriate unit tests to client and server code.





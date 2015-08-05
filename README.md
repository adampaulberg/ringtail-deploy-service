ringtail-deploy-service
===================

This project is a collection of tools that live on a Ringtail machine and facilitate installations and upgrades.  It consists of two major parts - a web-service API, and a set of loosely coupled command line utilities to do installations.

##Building Binaries
The first step is building the binaries.

Build the the solutions: RingtailAutoInstaller.sln and InstallerService.sln

Building these solutions will place the executables and all configuation files in the `bin` folder. 

`bin\InstallerService` contains the Service and WebAPI endpoint

`bin\AutoDeploy` contains the command line utilities used to deploy Ringtail


##Service Installation
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


##Service Configuration
The service uses a number of key-value pair configuration of various application subsets.

####InstallerService\upgrade.config

This config file uses the format
```
KEY|VALUE
```

*DROP_FOLDER*
The location where the service can self-update itself from. It is tied to the `/api/UpdateInstallerService` API. When configured and the `/api/UpdateInstallerService` is called it will retrieve the binaries at the specified path and overwrite the files currently installed on the machine. 

####InstallerService\config.config
This config uses the format
```
KEY|VALUE
```
*DeployPath*
The path where the deployment utility is installed. It should be configured to `C:\Upgrade\AutoDeploy` by default.  Changing this value has not been testing.

*MasterRunnerUser*, *MasterRunnerPass*
The user and password that the RingtailDeployService is running under.  In certain circumstances, the C# Process spawning mechanism was losing the Admin security token that the service is running under. This information is explicitly entered in this configuration to ensure that all spawned processes launch with the correct credentials and elevated security token.



####AutoDeploy\volitleData.config
This configures the variables used by the components needed to install Ringtail. This config uses the format:
```
App|KEY="VALUE"
```

##Security

The service can be configured to run in secure mode. When enabled, the following security measures are enabled for all connections to the API:

1. SSL connections are required and the service will listen on HTTPS instead of HTTP.
2. Basic Authentication is enabled against Active Directory users.
3. Authorization is only granted to users that are members of the Local Admin group.

The above measures secure connectivity to the API endpoints. The `AutoDeploy` and `InstallerService` folders can be restricted to only allow read access for Local Admins.  This prevents non-admins users from reading configuration files.

###Enabling Security

You can enable security by following these steps:

1. Modify `InstallerService\config.config` and set the `EnableSecurity|true`
2. Restart the RingtailDeployService
3. Generate a self-signed PKCS12 cert using OpenSSL (detailed instructions below)
4. Install the Certficate (detailed instructions below)
5. Bind the SSL Certificate to port 8080 (detailed instructions below)

#####3. Generate a self-signed PKCS12 cert using OpenSSL

For testing, I used a self-signed cert in PKCS12 format (.pkcs12 .pfx .p12). This format includes the Private Key as well as the Public Key. The following steps can be taken to generate a PKCS12 certificate using OpenSSL:

1. Generate the Private Key and Certificate in PEM format:

    ```
    openssl req -x509 -sha256 -nodes -days 1000 -newkey rsa:2048 -keyout privateKey.key -out certificate.crt
    ```
    
2. Convert the Private Key and Certficate into PKCS12

    ```
    openssl pkcs12 -export -out certificate.pfx -inkey privateKey.key -in certificate.crt
    ```

You can then install the certificate and bind it to the port using `netsh`

#####4. Installing the Certificate

To install the Certificate:

1. Open MMC
2. Add the Certificates Snap-In for the Computer Account for the Local Computer.
3. Import the .pfx file into the Personal\Certificates path.

#####5. Bind the SSL Certificate to port 8080

    ```
    netsh http add sslcert ipport=0.0.0.0:8080 certhash=e7ef4595e00fd4f46de23c1f0bc83d105df48405 appid="{b308a154-cfd1-443a-a47d-3008f12370c6}"
    ```
  
    The `certhash` property should match the Thumbprint of your certificate which can be obtained by viewing the properties of the Cert and removing the spaces. The `appid` property should be `{b308a154-cfd1-443a-a47d-3008f12370c6}`.

###Making secure network request to the service
Once security is enabled, you need to connect to endpoints with HTTPS.  You will also need to supply Basic Auth headers.  An example in cURL looks like:

```
$ curl --user "username:password" https://localhost:8080/api/help
```

You may also need to disable certificate checking if the certificate is self-signed.  An example in cURL looks like:

```
$ curl --insecure --user "username:password" https://localhost:8080/api/help
```


##Contributing

In lieu of a formal style guide, please maintain consistency with style and patterns in place in the application. Add appropriate unit tests to client and server code.





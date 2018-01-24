# Sample Azure Functions
A growing repository of rough examples on how to combine serverless functions with various services from the Microsoft Azure ecosystem. Most of these functions will focus on working with Blob Storage and Blob Triggers.

## Examples
* FaceDetect1 - A simple Blob Trigger-based function that demonstrates how to leverage the Face API within Microsoft's Cognitive Services to analyze jpg files as they are added or modified within a Blob container. This example also includes code touching on how to send SMS messages via Twilio.

## Setup Guide
The following steps cover the steps to create the necessary Azure resources required to deploy code from this repository into an Azure Function App.

1. Navigate to portal.azure.com in a web browser and sign in with a personal or corporate Microsoft account that has an active Azure subcription.

2. Open the Cloud Shell (look for the >_ button in the top toolbar).

3. If you've never used the shell before, select "Base (Linux)" when asked for a shell type and let the Cloud Shell setup create an Azure Storage account (scratch space) for you.

4. Run the rollowing command in the Cloud Shell to create a new Azure Resource Group:
```
az group create --name azure-function-examples-rg --location eastus
```

5. Run the following command in the Cloud Shell to create a new Azure Storage account, replacing <UNIQUENAME> with a unique storage account name. This name must ONLY contain lowercase letters and numbers.
```
az storage account create --name <UNIQUENAME> --location eastus --resource-group azure-function-examples-rg --sku Standard_LRS
```
Note: Store this unique storage account name in a text editor as it will be used in future steps and will also be used as the name of the Azure Function App and of the Cognitive Services deployment.

6. Run the following command in the Cloud Shell to get one of the access keys for this newly created storage account, replacing <UNIQUENAME> with the storage account name chosen in Step #5.
```
az storage account keys list --resource-group azure-function-examples-rg --account-name <UNIQUENAME>
```
Note: Store this key in a text editor as it will be used in future steps.
Note: This key will look something like this: HuZFVSV78wkjXeraYOO7hnz77iI7jvQal7UUMos+oSqq1nDsD15CeSTSRnu26NnB5w6tcTpiNIpypxCxKrNr3B==

7. Run the following command in the Cloud Shell to create a new Cognitive Services account based on Microsoft's Face API, replacing <UNIQUENAME> with the storage account name chosen in Step #5. Type 'y' when prompted with an information sharing warning.
```
az cognitiveservices account create --resource-group azure-function-examples-rg --name <UNIQUENAME> --sku S0 --kind Face --location eastus
```

8. Run the following command in the Cloud Shell to get one of the access keys for this newly created Cognitive Services account, replacing <UNIQUENAME> with the storage account name chosen in Step #5.
```
az cognitiveservices account keys list --resource-group azure-function-examples-rg --name <UNIQUENAME>
```
Note: This key will look something like this: 765d891feb778a6c9ca02126f9eca986

9. Run the following command in the Cloud Shell to create a Blob Container, replacing <CONTAINERNAME> with the name of a test container, <UNIQUENAME> with the storage account name chosen in Step #5, and <ACCOUNTKEY> with the key noted in Step #6.
```
az storage container create --name <CONTAINERNAME> --account-name <UNIQUENAME> --account-key <ACCOUNTKEY>
```
Note: Store <CONTAINERNAME> in a text editor as it will be used in future steps.

10. Run the following command in the Cloud Shell to create a new Azure Function App, replacing <UNIQUENAME> with the storage account name chosen in Step #5.
```
az functionapp create --name <UNIQUENAME> --storage-account <UNIQUENAME> --consumption-plan-location eastus --resource-group azure-function-examples-rg
```
Note: To keep things simple, let's use the storage account name as the name of the Function App. Function Apps require names that are unique across all Azure Web and Function Apps.

11.	Run the following command in the Cloud Shell to set necessary runtime variables that we will need for the sample Azure Function code. Replace <UNIQUENAME> with the storage account name chosen in Step #5, <BLOBCONTAINERNAME> with the blob container name set in Step #10, and <COGNITIVESERVICESKEY> with the Cognitive Services access key obtained in Step #8.
```
az functionapp config appsettings set --resource-group azure-function-examples-rg --name <UNIQUENAME> --settings "BlobContainerName=<BLOBCONTAINERNAME>", "CogSvcsKey=<COGNITIVESERVICESKEY>"
```
Note: Keep a close eye on typos here. There should be no spaces or punctuation around <BLOBCONTAINERNAME> or <COGNITIVESERVICESKEY>.

12. Run the following command in the Cloud Shell to pull the sample Azure Function code from GitHub, replacing <UNIQUENAME> with the storage account name chosen in Step #5.
```
az functionapp deployment source config --name <UNIQUENAME> --resource-group azure-function-examples-rg --repo-url https://github.com/mmarsala/azure-function-examples.git --branch master --manual-integration
```
Note: This command will take a few moments to run as it downloads several files from GitHub.

13. Use the search bar at the top of the Azure Portal to find the newly created Azure Function App.

14. Open up the list of Functions, the select the desired function (i.e. FaceDetect1) to view it's run.csx file and to expand the Logs section underneath the code. We will use this view to verify that the function is operating correctly.

## Disclaimer
This repository does not represent an official repository for Peer Software or Microsoft. All code and information in this repository is provided as is.
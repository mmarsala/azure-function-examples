# FaceDetect1

A simple Blob trigger-based function that demonstrates how to leverage the Face API within Microsoft's Cognitive Services to analyze jpg files as they are added or modified within a Blob container. This example also includes code touching on how to send SMS messages via Twilio. This function uses Microsoft CEO Satya Nadella as an example.

Setup Guide is provided here: https://github.com/mmarsala/azure-function-examples

The 12 steps of the Setup Guide above covers the creation of all necessary Azure resources as well as the deployment of this sample function from GitHub into an Azure Function App.

The following steps are meant to be a continuation of the Setup Guide, focusing on executing FaceDetect1:

13. Use the search bar at the top of the Azure Portal to find the newly created Azure Function App.

14. Open up the list of Functions, the select FaceDetect1 to view it's run.csx file and to expand the Logs section underneath the code. We will use this view to verify that the function is operating correctly.

15. Using a tool like Storage Explorer (https://azure.microsoft.com/en-us/features/storage-explorer/), navigate to your storage account then to the blob container created in Step #9 of the Setup Guide.

16. Download an image containing Satya to your computer. One example is: https://media.wired.com/photos/5926ee6af3e2356fd800aed2/1:1/w_2400,c_limit/MS-Linkedin-2016-06-12-1-c-1.jpg

17. Using the Storage Explorer tool (or another file to open object replication tool), copy this local jpg file into the container created in Step #9 of the Setup Guide.

18. Once the jpg is uploaded, the blob container triggers the start of the FaceDetect1 Azure Function.


## What the Function Does

1. Call the Detect function of the Face API within Cognitive Services to analyze a known image of Satya Nadella that is available on the Internet and create a profile for his face.
2. Use the same Detect function to look for all faces in the uploaded jpg file, creating a profile for each.
3. Use the Find Similars function of the Face API to compare the known facial profile of Satya against all discovered faces in the uploaded jpg file and report any matches.
4. Log out if any match is discovered with a confidence level greater than 50%. 

**Note:**  If the Azure Function does not trigger (no information is displayed in this Logs section), double check the values that were entered into the Cloud Shell during Step #11 of the Setup Guide. If a space or extra character is entered for either BlobContainerName or CogSvcsKey, the function will fail to run properly. Re-running the command in Step #11 of the Setup Guide with the correct information should fix the problem. Once that is done, use Storage Explorer or another tool to copy the existing jpg of Satya or upload a new one.


## Enabling Twilio Support
Twilio capabilities for sending SMS messages are already built into this sample Azure Function. If you would like to have the function send an SMS message when Satya is found in an image, you can do so by running the following Cloud Shell command to set several additional Function App variables. Four total variables are needed from Twilio, in addition to replacing <UNIQUENAME> with the storage account name chosen in Step #5 of the Setup Guide.

* '<TWILIOACCOUNTSID>' should be replaced with the Account SID that Twilio gives when you set up a new account. It should look something like: DebA341a1036553417802b8482ab944536
* '<TWILIOAUTHTOKEN>' is the authentication token that Twilio also gives you when you set up a new account. It should look something like this: 567222da7cb3643f21c1ca37cc4e951b
* '<TWILIOFROMNUMBER>' is the number from which Twilio will send the SMS. This is configured through your Twilio account. For a US-based number it should look something like this: +17037773344
* '<TWILIOTONUMBER>' is the number that will receive the SMS alert. This should be your cell phone. For a US-based number it should look something like this: +17037773344
```
az functionapp config appsettings set --resource-group azure-function-examples-rg -name <UNIQUENAME> --settings "TwilioAccountSid=<TWILIOACCOUNTSID>", "TwilioAuthToken=<TWILIOAUTHTOKEN>", "TwilioFromNumber=<TWILIOFROMNUMBER>", "TwilioToNumber=<TWILIOTONUMBER>"
```


## Disclaimer
This repository does not represent an official repository for Peer Software or Microsoft. All code and information in this repository is provided as is.
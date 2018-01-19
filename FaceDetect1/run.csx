#r "System.Web"
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Azure.Management.CognitiveServices;
using Microsoft.ProjectOxford.Face;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

/**
 * Basic Azure Function illustrating one small example of what can be done when on-premises file data is copied in near real-time to Azure Blob Storage in an open format.
 * This code requires several Azure resources be set up ahead of time. All necessary resources can be created and queried through the Azure Cloud Shell, 
 * using AZ bash commands that are outlined at: https://github.com/mmarsala/peerblobfunc1
 *
 * Once all Azure Cloud Shell CLI calls are successfully run, there should be no need to modify the C# code below. 
 * Error handling is limited so following the guide at the GitHub link above is crucial to the successful execution of this eaxmple.
 * 
 * This function activates on the putting or updating of a jpg in a specified blob container. Documentation on this trigger can be found here:
 * https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-blob 
 *
 * Once started, the Function uses Cognitive Services to look for a defined "known" face. Sample code here only works with a single "known" face but 
 * can be adapted to use Face Groups instead. Documentation on the Face API and Groups can be found here:
 * https://docs.microsoft.com/en-us/azure/cognitive-services/face/quickstarts/csharp
 *
 * This example also includes code for sending SMS notifications using Twilio. Doing so requires the creation of Twilio account. A few Azure CLI calls are required
 * to enable the Twilio logic. These calls are documented in the GitHub link above.
 * More information on using Twilio with Azure be found here: https://docs.microsoft.com/en-us/azure/twilio-dotnet-how-to-use-for-voice-sms
 *
 * @author Matt Marsala
 * Version: 003
 * Published: 17 Jan 2018
 *
 */
public static async Task<String> Run(Stream myBlob, String name, TraceWriter log) {

    //Pull blob container name and Cognitive Services Face API key from AppSettings. These should have already been 
    //set via the Azure Cloud Shell per the instructions in the GitHub link above.
    string blobContainerName = System.Environment.GetEnvironmentVariable("BlobContainerName", EnvironmentVariableTarget.Process);
    string cogSvcsKey = System.Environment.GetEnvironmentVariable("CogSvcsKey", EnvironmentVariableTarget.Process);

    string fullObjectPath =  blobContainerName + "/" + name + ".jpg";

    log.Info($"C# Blob trigger function...found new/updated blob\n Path:{fullObjectPath}");


    //Define name of known person and static web link to a picture of this person. For this example, let's use Satya Nadella.
    string knownFaceName = "Satya Nadella";
    string knownFaceImageLink = 
        "https://www.wired.com/wp-content/uploads/blogs/wiredenterprise/wp-content/uploads/2014/01/micro-soft-story.jpg";


    //Set up Http client to make calls to Face API
    var client = new HttpClient();
    var queryString = HttpUtility.ParseQueryString(string.Empty);
    log.Info("cogSvcsKey=" + cogSvcsKey);
    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", cogSvcsKey);


    //Define settings and URI for Face API calls
    queryString["returnFaceId"] = "true";
    queryString["returnFaceLandmarks"] = "false";
    queryString["returnFaceAttributes"] = "age,gender";//,headPose,smile,facialHair,glasses,emotion,hair,makeup,occlusion,accessories,blur,exposure,noise";
    var uri = "https://eastus.api.cognitive.microsoft.com/face/v1.0/detect?" + queryString;
    

    //First process known face, getting the bytes of the image and passing it to the Face API
    log.Info($"Processing known face...");

    HttpResponseMessage response;
    Request request = new Request();
    request.url = knownFaceImageLink;
    String jsonRequest = JsonConvert.SerializeObject(request);

    byte[] byteData = Encoding.UTF8.GetBytes(jsonRequest); 

    var content = new ByteArrayContent(byteData);
    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    response = await client.PostAsync(uri, content);

    string responseString = await response.Content.ReadAsStringAsync();


    //Get results of processing the known face, as well as its "ID"
    var resultOutput = JArray.Parse(responseString);
    FaceResult knownFace = resultOutput[0].ToObject<FaceResult>();
    string knownFaceId = knownFace.faceId;


    //Then look for faces in the new or modified jpg blob, getting the bytes of the image and passing it to the Face API
    log.Info($"Detecting faces in {fullObjectPath}...");

    byteData = ReadFully(myBlob);

    content = new ByteArrayContent(byteData);
    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
    response = await client.PostAsync(uri, content);

    responseString = await response.Content.ReadAsStringAsync();

    resultOutput = JArray.Parse(responseString);


    //Get a list of all faces detected in the new or modified jpg blob.
    PhotoResult report = new PhotoResult();
    report.Uri = fullObjectPath;
    
    List<FaceResult> faceList = new List<FaceResult>();

    foreach (var face in resultOutput) {
        faceList.Add(face.ToObject<FaceResult>());
    }

    report.Faces = (faceList.ToArray());

    log.Info("Facial Detection Report: " + report.ToString());


    //Set the known Face "ID" as well as each detected face in to the Find Similars API
    log.Info("Looking for faces...");

    FindSimilarRequest fsr = new FindSimilarRequest();
    fsr.faceId = knownFaceId;
    JArray faceIds = new JArray();
    JValue id;
    foreach (FaceResult face in faceList) {
        id = new JValue(face.faceId);
        faceIds.Add(id);
    }
    fsr.faceIds = faceIds;
 
    if (faceIds.Count == 0) {

        //No faces to work with, move on
        log.Info("No faces found in image, exiting... ");
    } else {

        //Faces found, build up the Http request to query Find Similars API
        string requestJson = JsonConvert.SerializeObject(fsr);

        uri = "https://eastus.api.cognitive.microsoft.com/face/v1.0/findsimilars";

        log.Info("Looking for known faces... (" + requestJson + ")");
        byteData = Encoding.UTF8.GetBytes(requestJson);
        content = new ByteArrayContent(byteData);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        response = await client.PostAsync(uri, content);

        responseString = await response.Content.ReadAsStringAsync();

        resultOutput = JArray.Parse(responseString);

        FindSimilarReport fsReport = new FindSimilarReport();
        fsReport.Uri = fullObjectPath;
        
        List<SimilarFace> findSimilarList = new List<SimilarFace>();

        //Iterate through the results of the Find Similars API call. If confidence of a match is 50% or higher, 
        //consider the known face to be within the new or modified blob jpg.
        bool triggerAlert = false;
        Double confidenceFactor = 0; 
        SimilarFace simFace;
        foreach (var face in resultOutput) {
            simFace = face.ToObject<SimilarFace>();
            if (simFace.confidence >= .5) {
                triggerAlert = true;
                confidenceFactor = simFace.confidence;
            }
            findSimilarList.Add(simFace);
        }

        fsReport.Faces = (findSimilarList.ToArray());

        log.Info("Find Similars Report: " + fsReport.ToString());

        if (triggerAlert) {
            //Known image found in new or modified blob jpg.
            var body = knownFaceName + " found in [" + fullObjectPath + "] with confidence of [" + confidenceFactor + "]!";

            log.Info("");
            log.Info(body);
            log.Info("");

            //Optionally, use Twilio to send a SMS message. If the following four AppSettings are not null, leverage the following code to send the SMS.
            //If any are null, bypass this logic.
            var accountSid = System.Environment.GetEnvironmentVariable("TwilioAccountSid", EnvironmentVariableTarget.Process);
            var authToken = System.Environment.GetEnvironmentVariable("TwilioAuthToken", EnvironmentVariableTarget.Process);
            var fromNumber = System.Environment.GetEnvironmentVariable("TwilioFromNumber", EnvironmentVariableTarget.Process);
            var toNumber = System.Environment.GetEnvironmentVariable("TwilioToNumber", EnvironmentVariableTarget.Process);

            if ( !string.IsNullOrEmpty(accountSid) && !string.IsNullOrEmpty(authToken) && 
                    !string.IsNullOrEmpty(fromNumber) && !string.IsNullOrEmpty(toNumber)) {
                TwilioClient.Init(accountSid, authToken);

                var message = await MessageResource.CreateAsync(
                    to: new PhoneNumber(toNumber),
                    from: new PhoneNumber(fromNumber),
                    body: body);

                log.Info("SMS sent [" + message.Sid + "]");
            } else {
                log.Info("Twilio settings missing, bypassing outgoing SMS.");
            }

        } else {
            log.Info("No known faces found!");
        }
    }

    return "Complete!";
} 

public static byte[] ReadFully(this Stream input) {
    using (MemoryStream ms = new MemoryStream())  {
        input.CopyTo(ms);
        return ms.ToArray();
    }
}

public class PhotoResult {
    public string Uri { get; set; }
    public FaceResult[] Faces;

    public override string ToString() {
        return JsonConvert.SerializeObject(this);
    }
}

public class FaceResult {
    public string faceId { get; set; }
    public FaceRectangle faceRectangle { get; set; }
    public string gender { get; set; }
    public Double age { get; set; }
    public FaceFacialHair facialHair { get; set; }
    public string glasses { get; set; }
    public FaceEmotion emotion { get; set; }
}

public class FaceRectangle {
    public int top { get; set; }
    public int left { get; set; }
    public int width { get; set; }
    public int height { get; set; }
}

public class FaceFacialHair {
    public Double moustache { get; set; }
    public Double beard { get; set; }
    public Double sideburns { get; set; }
}

public class FaceEmotion {
    public Double anger { get; set; }
    public Double contempt { get; set; }
    public Double disgust { get; set; }
    public Double fear { get; set; }
    public Double happiness { get; set; }
    public Double neutral { get; set; }
    public Double sadness { get; set; }
    public Double surprise { get; set; }
}

public class Request {
    public string url { get; set; }
}

public class FindSimilarRequest {
    public string faceId { get; set; }
    public JArray faceIds { get; set; }
}

public class FindSimilarReport {
    public string Uri { get; set; }
    public string searchId { get; set; }
    public SimilarFace[] Faces;

    public override string ToString() {
        return JsonConvert.SerializeObject(this);
    }
}

public class SimilarFace {
    public string faceId { get; set; }
    public Double confidence { get; set; }
}
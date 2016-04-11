# Global Azure Bootcamp 2016 - Service Fabric

[slides](gab-2016-sf.pptx)

## Lab Instructions

The WordCount sample app provides an introduction to using reliable collections and to partitioning stateful services. A client-side JavaScript function generates random five-character strings, which are then sent to the application via an ASP.NET WebAPI to be counted. The stateless web service resolves the endpoint for the stateful service's partition based on the first character of the string. The stateful service maintains a backlog of words to count in a 'ReliableQueue' and then keeps track of their count in a 'ReliableDictionary'. The total count, plus a per-partition count, are shown in the web UI at [http://localhost:8081/WordCount/](http://localhost:8081/WordCount/) (for a locally deployed app instance).

### Pre-requisites

1. VS2015 Community Edition - download [here](https://www.visualstudio.com/en-us/products/visual-studio-community-vs.aspx) and install
2. Azure SDK - download [here](https://go.microsoft.com/fwlink/?linkid=518003&clcid=0x409) and install (be sure you have the latest version)
3. Service Fabric SDK - download [here](http://www.microsoft.com/web/handlers/webpi.ashx?command=getinstallerredirect&appid=MicrosoftAzure-ServiceFabric-VS2015) and install (be sure you have the latest version)
4. Enable Powershell script execution as per [here](https://azure.microsoft.com/en-us/documentation/articles/service-fabric-get-started/)
5. Clone or download lab code [here](https://github.com/jplane/Global-Azure-Bootcamp-2016)

### Review the application code

1. Open Visual Studio 2015 with admin privileges, by right-clicking the icon and selecting 'Run as Administrator'
2. Open the WordCount.sln solution file
3. Note that there are 4 projects in the solution:
 - WordCount.Service is a [stateful](https://azure.microsoft.com/en-us/documentation/articles/service-fabric-reliable-services-reliable-collections/) Service Fabric service that provides the core word counting functionality for the application. It is partitioned to enhance scalability and fault tolerance.
 - WordCount.WebService is a stateless Service Fabric service that exists to a) serve index.html for viewing application activity and b) forward REST requests from index.html to the stateful WordCount service (above). As it maintains no state it has no need for multiple partitions (can you think why that is?).
 - WordCount.Common is a shared library that defines common OWIN HTTP listener logic, used by both SF services. Note that SF code is not hosted within IIS or another 'off-the-shelf' web server, so any desired HTTP endpoints must be self-hosted (using OWIN or similar HTTP stack).
 - WordCount is the 'root' Service Fabric project that defines the application and associates the two core services with it.
 - In WordCount.WebService:
    - Open the [WordCountWebService.cs](Lab - WordCount/WordCount.WebService/WordCountWebService.cs) file. This class defines the stateless service (note its base class of StatelessService). CreateServiceInstanceListeners() is called during startup by the SF runtime and here is used to expose REST endpoints via an OWIN listener.
    - Open the [Startup.cs](Lab - WordCount/WordCount.WebService/Startup.cs) file. This class is wired in during OWIN bootstrapping and defines custom logic for exposing the Web API controller and for serving index.html.
    - Open the [Program.cs](Lab - WordCount/WordCount.WebService/Program.cs) file. This is the main entrypoint of this SF service; note that it's simply a console app that talks to SF and bootstraps the service type. All SF applications are defined as console EXEs.
    - Open the [wwwroot/index.html](Lab - WordCount/WordCount.WebService/wwwroot/Index.html) file. This simple HTML file displays current application activity, in the form of words added, partition key chosen for each word, and aggregate word count stats across all partitions in the cluster.
    - Open the [Controllers/DefaultController.cs](Lab - WordCount/WordCount.WebService/Controllers/DefaultController.cs) file. This is a standard Web API controller that exposes REST endpoints for getting word count stats as well as adding new words to the application. Internally, for each request, it resolves the appropriate SF partition to talk to, and forwards REST requests to the appropriate partition-specific endpoint.
 - In WordCount.Service:
    - Open the [WordCountService.cs](Lab - WordCount/WordCount.Service/WordCountService.cs) file. This class defines the stateful service (note its base class of StatefulService). CreateServiceReplicaListeners() is called during startup by the SF runtime and here is used to expose REST endpoints via an OWIN listener. This class also defines logic in RunAsync() for listening on a reliable queue (that is, a queue whose state is replicated to all nodes where this service is running in the cluster). Items in the queue correspond to new words added to the application, and are processed by adding them to reliable dictionary instances (which are also replicated across the cluster).
    - Open the [Startup.cs](Lab - WordCount/WordCount.Service/Startup.cs) file. This class is wired in during OWIN bootstrapping and defines custom logic for exposing the Web API controller.
    - Open the [Program.cs](Lab - WordCount/WordCount.Service/Program.cs) file. This is the main entrypoint of this SF service; note that it's simply a console app that talks to SF and bootstraps the service type.
    - Open the [Controllers/DefaultController.cs](Lab - WordCount/WordCount.Service/Controllers/DefaultController.cs) file. This is a standard Web API controller that exposes REST endpoints for getting word count stats as well as adding new words to the application. Internally the controller reads data from a reliable dictionary (for Count()) and queues new words to a reliable queue (for AddWord()). Note that the queue listener logic is defined in WordCountService.RunAsync()... can you think of a reason why a queue is used here, instead of simply moving the RunAsync() logic to DefaultController.AddWord()?

### Review Service Fabric Explorer

Now open the [SF Service Fabric Explorer UI](http://localhost:19080/Explorer/). This gives you visibility into the application activity on and overall health of your Service Fabric cluster. You can view aggregated cluster stats and diagnostics by clicking on the 'Cluster' root on the left-hand tree. You can view stats grouped by logical application by drilling into the 'Applications' subtree. Finally, you can view stats related to physical cluster nodes by drilling into the 'Nodes' subtree.

For a visualization of your cluster's Fault and Upgrade Domains, and the physical node mappings to these, click on 'Cluster' (in the left tree) and then 'Cluster Map' on the right. These mappings are the default and can be customized in a real-world Service Fabric deployment. To view the metadata used to create this default cluster configuration, click 'Manifest'; again, these settings can be modified extensively in a real-world deployment scenario.

### F5 build/deploy/debug on local cluster

Leave the Service Fabric Explorer open, and now let's debug the SF code. Visual Studio integrates nicely with Service Fabric, such that simply hitting F5 to debug the code will build and deploy to your local SF cluster, and allow you to set breakpoints and otherwise have the normal VS.NET debug experience you know and love.

Hit F5 and notice the build and deployment output in the Output window of Visual Studio (handy for diagnosing deployment problems!). Once the application is up and running, notice that Service Fabric diagnostic information is piped to the Visual Studio Diagnostic window... since the SF application is written to write diagnostic info, you get rich telemetry on your running app right within Visual Studio (in a production scenario you can write this information to other, durable storage types).

Once your app is running, switch to Service Fabric Explorer and (assuming all is well) watch the application count increase by 1 as VS.NET deploys your code into the cluster. In the left-hand tree you should see a new 'WordCount' application type, and an instance of the app below that (with SF, you can deploy multiple instances of the same app type, each with configuration specific to a given scenario... think of multi-tenant scenarios, etc.).

Drilling deeper into the Service Fabric Explorer tree, you'll see the two service instances listed, corresponding to the stateless web service and the stateful service. Notice also that the stateful WordCountService is configured to run in two partitions (each with three physical replicas), while the stateless WordCountWebService runs in one partition (with a single physical instance). See if you can piece together how this configuration comes together (and how you might change it yourself) using the following config files:

- [WordCount/ApplicationPackageRoot/ApplicationManifest.xml](Lab - WordCount/WordCount/ApplicationPackageRoot/ApplicationManifest.xml)
- [WordCount/ApplicationParameters/Local.xml](Lab - WordCount/WordCount/ApplicationParameters/Local.xml)
- [WordCount/ApplicationParameters/Cloud.xml](Lab - WordCount/WordCount/ApplicationParameters/Cloud.xml)
- [WordCount.WebService/PackageRoot/ServiceManifest.xml](Lab - WordCount/WordCount.WebService/PackageRoot/ServiceManifest.xml)
- [WordCount.Service/PackageRoot/ServiceManifest.xml](Lab - WordCount/WordCount.Service/PackageRoot/ServiceManifest.xml)

To see the application in action, browse to [http://localhost:8081/wordcount/](http://localhost:8081/wordcount/). While the debugger is still attached, place a breakpoint somewhere in the controller classes in either the web service or the stateful service project (or perhaps in WordCountService.RunAsync()), and step through the code for a given request to see how the magic happens (to be clear, the actual 'business logic' is fairly vanilla stuff... the real magic here is the power and flexibility of the Service Fabric runtime). 

### Update and upgrade

So far, we've deployed v1 of the WordCount application... but one of the powerful capabilities of Service Fabric is it's first class support for in-place service upgrades that minimize or eliminate downtime for current users. Let's modify the application by adding a new feature, and then deploy v2.

The new feature will be a display of words added more than once to the application. Here are the steps to make this change (look for TODO comments in the code as placeholders, and if at any time you get lost or need some help, refer to the finished lab code [here](Lab - WordCount (complete))):

1. In [WordCount.Service/Controllers/DefaultController.cs](Lab - WordCount/WordCount.Service/Controllers/DefaultController.cs) you'll need to add a new service method FindDups() that examines the 'wordCountDictionary' and returns a list of all words with count > 1. Look at WordCountService.RunAsync() to see how to reference the dictionary. If you need help enumerating the values within, check the completed lab code for help (it's slightly tricky since the dictionary supports only async operations).

2. In [WordCount.WebService/Controllers/DefaultController.cs](Lab - WordCount/WordCount.WebService/Controllers/DefaultController.cs) you'll need to add a new service method FindDups() that enumerates all service partitions, invokes the FindDups() service method you just added on each partition, and returns HTML to display all duplicates for all partitions. See the Count() method in this class for how to enumerate partitions, and of course check the completed lab code if you get stuck.

3. In [WordCount.WebService/wwwroot/index.html](Lab - WordCount/WordCount.WebService/wwwroot/Index.html) you'll need to add a new FindDups() JavaScript method that invokes the new stateless web service FindDups API. You'll also need to add a timer to invoke the FindDups() JS method every 500 milliseconds or so. Next you'll want to add a new DIV at the bottom of the page to hold the HTML for the duplicate results. Finally, to increase the likelihood of duplicates appearing in the results, modify the JavaScript that produces the random input strings to produce strings of length 3 instead of 5.

Once you've made these changes, build your code in Visual Studio and then right-click the 'WordCount' project node and select 'Publish'.

![Right-click Publish](/images/publish.png)

You'll see the following dialog appear. Make sure you set the topmost dropdown to Local.xml, and that you check the 'Upgrade the Application' checkbox. Finally, click on the 'Manifest Versions...' button:

![Publish dialog](/images/publish-dialog.PNG)

In the manifest versions dialog, you'll need to increment the version numbers of the updated service code (since you added a new API to each service, you'll want to increment the version number of each). Ensure the 'Automatically update application and service versions' checkbox is checked, and expand the 'WordCountWebServicePkg' and 'WordCountServicePkg' nodes in the dialog. Update the 'New Version' setting to 2.0.0 for the 'Code' element under each (leave the 'Config' version under 'WordCountWebServicePkg' unchanged, as you didn't updated that). Note that when you update the Code version, the corresponding Service and Application versions update, too (because of the checkbox setting). The final settings should look something like this:

![Update manifest versions](/images/manifest-versions.PNG)

Click 'Save' to close out this dialog, and then click 'Publish' on the main dialog. Switch to Service Fabric Explorer in the browser and click on the 'fabric:/WordCount' Application node in the left-hand tree. You should see Service Fabric incrementally updating all Update Domains affected by the update operation; you should also see corresponding feedback in Visual Studio marking progress of the update. When the update is complete, refresh or re-browse to [http://localhost:8081/wordcount/](http://localhost:8081/wordcount/) to see the new duplicate detection feature you just added.

## More information

- [Samples](http://aka.ms/servicefabricsamples)
- [More info on programming models](https://azure.microsoft.com/en-us/documentation/articles/service-fabric-choose-framework/)
- [More info on upgrades](https://azure.microsoft.com/en-us/documentation/articles/service-fabric-application-upgrade-tutorial/)
- [All SF docs](http://aka.ms/servicefabricdocs)

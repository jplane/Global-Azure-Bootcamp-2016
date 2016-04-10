# Global Azure Bootcamp 2016 - Service Fabric

[slides][gab2016 - service fabric.pptx]

## Lab Instructions

The WordCount sample app provides an introduction to using reliable collections and to partitioning stateful services. A client-side JavaScript function generates random five-character strings, which are then sent to the application via an ASP.NET WebAPI to be counted. The stateless web service resolves the endpoint for the stateful service's partition based on the first character of the string. The stateful service maintains a backlog of words to count in a 'ReliableQueue' and then keeps track of their count in a 'ReliableDictionary'. The total count, plus a per-partition count, are shown in the web UI at http://localhost:8081/WordCount/ (for a locally deployed app instance).

### Pre-requisites

1. VS2015 Community Edition - download [here](https://www.visualstudio.com/en-us/products/visual-studio-community-vs.aspx) and install
2. Azure SDK - download [here](https://go.microsoft.com/fwlink/?linkid=518003&clcid=0x409) and install (be sure you have the latest version)
3. Service Fabric SDK - download [here](http://www.microsoft.com/web/handlers/webpi.ashx?command=getinstallerredirect&appid=MicrosoftAzure-ServiceFabric-VS2015) and install (be sure you have the latest version)
3. Clone or download lab code [here]()

### Review the application code

1. 

### Review the Service Fabric configuration

### Review Service Fabric Cluster Manager

### F5 build/deploy/debug on local cluster

### Update and upgrade

## More information

- [Samples](http://aka.ms/servicefabricsamples)
- [More info on programming models](https://azure.microsoft.com/en-us/documentation/articles/service-fabric-choose-framework/)
- [More info on upgrades](https://azure.microsoft.com/en-us/documentation/articles/service-fabric-application-upgrade-tutorial/)
- [All SF docs](http://aka.ms/servicefabricdocs)

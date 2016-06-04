##General Info
M.O.U.S.E v2 - better, faster, runs on Azure Service Fabric...

Documentation is WIP, so for now look into:
* Samples/BasicChat - non scalable chat server showcasing usage of basic networking with NetNode/NetChannel
* Samples/ActorChat - scalable actor based chat with rooms with with 3 swappable backends:
  * pure ServiceFabric Actors
  * MOUSE Actors over UDP transport with ServiceFabric naming service for actor distribution/registry (at most )
  * MOUSE Actors over hybrid EventHub/UDP transport with AzureStorage blob leasing + EventHub partitions for actor distribution/registry
* Tests/PerormanceTesting/ - for ServiceFabric actors VS Orleans VS MOUSE actors standoff    

###Dependencies
* ServiceFabric in Azure: use Provisioning/ServiceFabric ARM project 
* Monitoring in Azure(ElasticSearch/Grafana/Kibana) : use Provisioning/Monitoring ARM project
* Performance testing uses MBrace: easieast way is to provision it with https://github.com/mbraceproject/MBrace.StarterKit  

## License - Mit





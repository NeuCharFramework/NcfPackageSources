# Local branch
  
The Local branch is mainly responsible for handling communication between various modules or areas within the system. It implements collaboration between these modules or areas through exposed interfaces, thereby ensuring decoupling and easy extension between them. Typically, Local branches are implemented within the same system or service and do not involve communication across systems or services.

In Local,`AppService`Responsible for handling communication between modules, and NCF uses`Dynamic WebApi`, can be`AppService`The method is automatically generated for use as a WebApi service without having to`Remote`Manually write the WebApi service in the branch.

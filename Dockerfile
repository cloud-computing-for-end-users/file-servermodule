# used for creating the docker image that will have the server module

# this is a base ubuntu image with .Net Core 2.2 installed
FROM gaticks/bachelor-project:BaseUbuntuNetCore2.2 as base 

# copy application 
COPY /src/file-servermodule/bin/Debug/netcoreapp2.2/publish/ /data/file-servermodule/


ENTRYPOINT ["dotnet", "/data/file-servermodule/file-servermodule.dll"]

#CMD ["/bin/bash"]

CMD ["rcp:5522", "rrp:5523", "isLocal:false", "rip:172.17.0.2"]




########FROM SO-MODULE#####3
# first three arguments are for self setup, last two are for infromation on the router the module connectes to
#currently ip to connect to host is 172.17.0.7
#currently ip to connect to container is 172.29.64.1

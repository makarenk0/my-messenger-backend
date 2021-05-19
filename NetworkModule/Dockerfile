FROM mcr.microsoft.com/dotnet/runtime:5.0
RUN apt-get update
RUN apt-get -qq -y install wget
RUN apt-get -qq -y install sudo
RUN apt-get -y install gnupg
RUN apt-get -y install gnupg1
RUN apt-get -y install gnupg2
COPY bin/Release/net5.0/linux-x64/ NetworkModule/
WORKDIR /NetworkModule
EXPOSE 80/tcp
EXPOSE 20/tcp
ENTRYPOINT ["dotnet", "NetworkModule.dll"]
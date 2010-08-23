==License==
New BSD License

Copyright (c) 2009, http://www.realxtend.org/
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.
    * Neither the name of the realXtend nor the
      names of its contributors may be used to endorse or promote products
      derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY EXPRESS OR
IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE
GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER
IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

==Building ModreX==

===Build reqirements===

====Windows====

Only requirement for Windows is the build tool. Supported build tools are Microsoft Visual C# 2008, mono and nant.

http://www.microsoft.com/express/vcsharp/
http://www.mono-project.com/
http://nant.sourceforge.net/

====Linux====

See: http://opensimulator.org/wiki/Dependencies#Linux and http://opensimulator.org/wiki/Build_Instructions#Linux.2FMac_OS_X.2FFreeBSD


===Getting the source and compiling===

 1. To start the build process, first get the latest OpenSim source codes. More information how to get OpenSim source code, see: http://opensimulator.org/wiki/Download

 2. After getting the OpenSim source, get the ModreX sources. To do this, follow these instructions:
  * Navigate to addon-modules directory under OpenSim trunk
  * Check out the ModreX source to directory ModreX with command: git clone git://github.com/mikkopa/ModreX.git ModreX
  * Note: it is very important that you check out the source to precisely the "addon-modules/ModreX" directory (not to be confused with "bin/addon-modules/ModreX")! If this is not done properly next steps will not work.
  * Note: you can also use the above repository as git submodule. For more information how to use git submodules, see: http://www.kernel.org/pub/software/scm/git/docs/git-submodule.html

 3. Now before building, you must run the prebuilding script that will create the project files from the prebuild.xml. Go to OpenSim trunk root directory and run the runprebuild.bat or ./runprebuild.sh depending from the operating system you are using.

 4. Now the project files should be successfully built and the project is ready for building.
  * With Visual C#, open OpenSim.sln and build it. ModreX projects should be inside the OpenSim solution.
  * With mono/nant, run nant

 5. After successful build, some dependency files should have been automaticly copied to bin/ScriptEngines/Lib and bin/ScriptEngines/PythonScript directories. If they however do not exist, manually copy them from addon-modules/ModreX/ModularRex/ScriptEngines/.

==Configuring ModreX==
===ModreX configurations===
Configurations should be copied to bin/addon-modules/ModreX/config/modrex.ini during during the build. If the file doesn't exist, manually copy file from ModreX/ModularRex/addon-modules/ModreX/config/modrex.ini. See the modrex.ini for all the configs.

===Remove NHibernate debug info from logger===
To remove NHibernate debug infromation from console and file logs, edit file OpenSim.exe.config or  OpenSim.32BitLaunch.exe.config depending which binary do you run. Add these lines to file before root element:
    <!-- if you'd like to know what NHibernate is doing more set value="DEBUG" -->

    <logger name="NHibernate" additivity="false">

      <level value="INFO"/>

      <appender-ref ref="NHibernateFileLog"/>

    </logger>


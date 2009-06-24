==NANT==
See: readme.linux.txt

==Visual Studio==

Note for 64bit users: ModularRex.Tools.MigrationTool.csproj uses SQLite which doesn't work if the project is compiled in "Any Cpu" mode. To change mode to x86, right click project in visual studio, select properties, from build tab, change platform target to x86.

===To compile and use ModreX===
1. First get the OpenSim source and place it for example to directory opensim/
2. Go to that directory, run the runprebuild script.
3. Open the created solution file and compile it.
4. Create new directory opensim/modrex
5. Checkout the ModreX source to that.
6. Run the runprebuild script from opensim/modrex/ModularRex
7. Copy directories Lib and PythonScript from opensim/modrex/ModularRex/RexParts/RexPython/Resources to opensim/bin/ScriptEngines
8. Open the newly created ModularRex.sln and compile

Now the ModreX binaries have been placed in to opensim/bin and is ready to run.

===To compile and use ModreX from source in debug mode===
1. First get the OpenSim source and place it for example to directory opensim/
2. Go to that directory, run the runprebuild script.
3. Open the created solution file and compile it.
4. Create new directory opensim/modrex
5. Checkout the ModreX source to that.
6. Run the runprebuild script from opensim/modrex/ModularRex
7. Copy directories Lib and PythonScript from opensim/modrex/ModularRex/RexParts/RexPython/Resources to opensim/bin/ScriptEngines
8. Go back to OpenSim solution and right click the solution and select "Add -> Existing project...". Add these six projects:
  * opensim/modrex/ModularRex/ModularRex.csproj
  * opensim/modrex/ModularRex/NHibernate/ModularRex.NHibernate.csproj
  * opensim/modrex/ModularRex/RexFramework/ModularRex.RexFramework.csproj
  * opensim/modrex/ModularRex/RexOdePlugin/ModularRex.RexOdePlugin.csproj
  * opensim/modrex/ModularRex/RexBot/ModularRex.RexBot.csproj (optional)
  * opensim/modrex/ModularRex/Tools/MigrationTool/ModularRex.Tools.MigrationTool.csproj (optional)
9. Compile and have fun
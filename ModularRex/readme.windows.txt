==NANT==
See: readme.linux.txt

==Visual Studio==

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
8. Go back to OpenSim solution and right click the solution and select "Add -> Existing project...". Add these five projects:
  * opensim/modrex/ModularRex/ModularRex.csproj
  * opensim/modrex/ModularRex/ModularRex.NHibernate.csproj
  * opensim/modrex/ModularRex/ModularRex.RexFramework.csproj
  * opensim/modrex/ModularRex/ModularRex.RexOdePlugin.csproj
  * opensim/modrex/ModularRex/ModularRex.RexBot.csproj
9. Compile and have fun
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Linq;
using System.Reflection;


namespace SourceGenerators.Test
{
    [TestClass]
    public class PropertyNotifyGeneratorTests
    {
        [TestMethod]
        public void SimpleGeneratorTest()
        {
            // Create the 'input' compilation that the generator will act on
            Compilation inputCompilation = CreateCompilation(@"
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using PropertyNotify;

namespace Demo.Testing
{
    partial class MagicBall
    {
        [PropertyNotify]
        int position = -2; //in lightyears
        [PropertyNotify]
        int size = 9000; //in centimeters
        [PropertyNotify]
        string name = ""Kevin""; //in english
    }
}
");

            // directly create an instance of the generator
            // (Note: in the compiler this is loaded from an assembly, and created via reflection at runtime)
            PropertyNotifyGenerator generator = new PropertyNotifyGenerator();

            // Create the driver that will control the generation, passing in our generator
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Run the generation pass
            // (Note: the generator driver itself is immutable, and all calls return an updated version of the driver that you should use for subsequent calls)
            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);


            var generatedCode = driver.GetRunResult().Results[0].GeneratedSources;
            foreach(var result in generatedCode)
            {
                var code = result.SourceText;
            }
            

            // We can now assert things about the resulting compilation:
            Debug.Assert(diagnostics.IsEmpty); // there were no diagnostics created by the generators
            Debug.Assert(outputCompilation.SyntaxTrees.Count() == 3); // we have two syntax trees, the original 'user' provided one, and the one added by the generator
            var diag = outputCompilation.GetDiagnostics();
            
            if (false) // ignore this
                Debug.Assert(diag.IsEmpty); // verify the compilation with the added source has no diagnostics  

            // Or we can look at the results directly:
            GeneratorDriverRunResult runResult = driver.GetRunResult();

            // The runResult contains the combined results of all generators passed to the driver
            Debug.Assert(runResult.GeneratedTrees.Length == 2);
            Debug.Assert(runResult.Diagnostics.IsEmpty);

            // Or you can access the individual results on a by-generator basis
            GeneratorRunResult generatorResult = runResult.Results[0];
            Debug.Assert(generatorResult.Generator == generator);
            Debug.Assert(generatorResult.Diagnostics.IsEmpty);
            Debug.Assert(generatorResult.GeneratedSources.Length == 1);
            Debug.Assert(generatorResult.Exception is null);
        }

        private static Compilation CreateCompilation(string source)
            => CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source) },
                new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.ConsoleApplication));
    }
}
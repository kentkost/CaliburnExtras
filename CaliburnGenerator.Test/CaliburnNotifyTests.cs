using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using CaliburnExtras;

namespace CaliburnGenerator.Test
{
    [TestClass]
    public class CaliburnNotifyTests
    {
        [TestMethod]
        public void SimpleGeneratorTest()
        {
            // Create the 'input' compilation that the generator will act on
            Compilation inputCompilation = CreateCompilation(@"
using ApiClients.CompanyAPI;
using ApiClients.EconomicAPI;
using ApiClients.WhitelabelAPI;
using Caliburn.Micro;
using CaliburnExtras;
using CommonAPITools.DTOs;
using ContractRating.Contracts.DTOs;
using ContractRating.Contracts.Enums;
using CustomerContracts.Interfaces.Services;
using CustomerContracts.Model;
using CustomerContracts.Services;
using Events;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Translations;
using WpfControls;
using WPFDialogs.Interfaces;
using WPFDialogs.Presentation;
using MessageBox = System.Windows.MessageBox;

namespace CustomerContracts.ViewModels
{
    public partial class OverdraftInvocingViewModel : CommonListViewModel
    {
        private readonly IDialogHandler dialogHandler;
        private readonly IEconomicProductClient economicProductClient;
        private readonly IOverdraftReportService overdraftReportService;
        private readonly IContractDialog contractDialog;
        private readonly IWLOperatorClient wlOperatorClient;
        private readonly IERPCompanyClient companyClient;
        private readonly ICustomerContractsService customerContractsService;
        private readonly List<OverdraftType> supportedPDFTypes = new List<OverdraftType> { OverdraftType.USAGE, OverdraftType.COMBINED };

        public OverdraftInvocingViewModel(IConfiguration configuration, IEventAggregator eventAggregator, IDialogHandler dialogHandler, IEconomicProductClient economicProductClient,
            IOverdraftReportService overdraftReportService, IContractDialog contractDialog, IWLOperatorClient wlOperatorClient, IERPCompanyClient companyClient, 
            ICustomerContractsService customerContractsService)
            : base(configuration, eventAggregator)
        {
            this.dialogHandler = dialogHandler;
            this.economicProductClient = economicProductClient;
            this.overdraftReportService = overdraftReportService;
            this.contractDialog = contractDialog;
            this.wlOperatorClient = wlOperatorClient;
            this.companyClient = companyClient;
            this.customerContractsService = customerContractsService;
        }

        [CaliburnNotify(""Hello"", ""FixMe"")]
        private OverdraftReport selectedReport;
    }
}
");

            // directly create an instance of the generator
            // (Note: in the compiler this is loaded from an assembly, and created via reflection at runtime)
            CaliburnNotifyGenerator generator = new CaliburnNotifyGenerator();

            // Create the driver that will control the generation, passing in our generator
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Run the generation pass
            // (Note: the generator driver itself is immutable, and all calls return an updated version of the driver that you should use for subsequent calls)
            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);


            var generatedCode = driver.GetRunResult().Results[0].GeneratedSources;
            foreach (var result in generatedCode)
            {
                var code = result.SourceText;
            }


            // We can now assert things about the resulting compilation:
            Debug.Assert(diagnostics.IsEmpty); // there were no diagnostics created by the generators
            Debug.Assert(outputCompilation.SyntaxTrees.Count() == 2); // we have two syntax trees, the original 'user' provided one, and the one added by the generator
            var diag = outputCompilation.GetDiagnostics();

            if (false)
            {
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
        }

        private static Compilation CreateCompilation(string source)
            => CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source) },
                new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.ConsoleApplication));
    }
}
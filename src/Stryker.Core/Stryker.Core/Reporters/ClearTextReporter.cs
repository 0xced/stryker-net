using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Crayon;
using Stryker.Core.Mutants;
using Stryker.Core.Options;
using Stryker.Core.ProjectComponents;

namespace Stryker.Core.Reporters
{
    /// <summary>
    /// The clear text reporter, prints a table with results.
    /// </summary>
    public class ClearTextReporter : IReporter
    {
        private readonly StrykerOptions _options;
        private readonly TextWriter _consoleWriter;

        public ClearTextReporter(StrykerOptions strykerOptions, TextWriter consoleWriter = null)
        {
            _options = strykerOptions;
            _consoleWriter = consoleWriter ?? Console.Out;
        }

        public void OnMutantsCreated(IReadOnlyProjectComponent reportComponent)
        {
            // This reporter does not report during the testrun
        }

        public void OnStartMutantTestRun(IEnumerable<IReadOnlyMutant> mutantsToBeTested)
        {
            // This reporter does not report during the testrun
        }

        public void OnMutantTested(IReadOnlyMutant result)
        {
            // This reporter does not report during the testrun
        }

        public void OnAllMutantsTested(IReadOnlyProjectComponent reportComponent)
        {
            var files = reportComponent.GetAllFiles();

            if (files.Any())
            {
                // print empty line for readability
                _consoleWriter.WriteLine();
                _consoleWriter.WriteLine();
                _consoleWriter.WriteLine("All mutants have been tested, and your mutation score has been calculated");

                var filePathLength = Math.Max(9, files.Max(f => f.RelativePath?.Length ?? 0) + 1);
                string dashes = new string('─', filePathLength);
                _consoleWriter.WriteLine($"┌─{dashes}┬──────────┬──────────┬───────────┬────────────┬──────────┬─────────┐");
                _consoleWriter.WriteLine($"│ File{new string(' ', filePathLength - 4)}│  % score │ # killed │ # timeout │ # survived │ # no cov │ # error │");
                _consoleWriter.WriteLine($"├─{dashes}┼──────────┼──────────┼───────────┼────────────┼──────────┼─────────┤");

                DisplayComponent(reportComponent, filePathLength);

                foreach (var file in files)
                {
                    DisplayComponent(file, filePathLength);
                }

                _consoleWriter.WriteLine($"└─{dashes}┴──────────┴──────────┴───────────┴────────────┴──────────┴─────────┘");
            }
        }

        private void DisplayComponent(IReadOnlyProjectComponent inputComponent, int filePathLength)
        {
            _consoleWriter.Write($"│ {(inputComponent.RelativePath ?? "All files").PadRight(filePathLength)}│ ");

            var mutationScore = inputComponent.GetMutationScore();

            if (inputComponent.IsComponentExcluded(_options.Mutate))
            {
                _consoleWriter.Write(Output.Bright.Black("Excluded"));
            }
            else if (double.IsNaN(mutationScore))
            {
                _consoleWriter.Write(Output.Bright.Black("     N/A"));
            }
            else
            {
                var scoreText = $"{mutationScore * 100:N2}".PadLeft(8);

                var checkHealth = inputComponent.CheckHealth(_options.Thresholds);
                if (checkHealth is Health.Good)
                {
                    _consoleWriter.Write(Output.Green(scoreText));
                }
                else if (checkHealth is Health.Warning)
                {
                    _consoleWriter.Write(Output.Yellow(scoreText));
                }
                else if (checkHealth is Health.Danger)
                {
                    _consoleWriter.Write(Output.Red(scoreText));
                }
            }

            var mutants = inputComponent.Mutants.ToList();
            _consoleWriter.Write($" │ {mutants.Count(m => m.ResultStatus == MutantStatus.Killed),8}");
            _consoleWriter.Write($" │ {mutants.Count(m => m.ResultStatus == MutantStatus.Timeout),9}");
            _consoleWriter.Write($" │ {inputComponent.TotalMutants().Count() - inputComponent.DetectedMutants().Count(),10}");
            _consoleWriter.Write($" │ {mutants.Count(m => m.ResultStatus == MutantStatus.NoCoverage),8}");
            _consoleWriter.Write($" │ {mutants.Count(m => m.ResultStatus == MutantStatus.CompileError),7}");
            _consoleWriter.WriteLine($" │");
        }
    }
}

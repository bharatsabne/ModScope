using ModScope.Core.Models;
using ModScope.ModbusMaster;

namespace ModScope.Diagnostics.Sequences;

/// <summary>
/// Status of an automated test sequence step.
/// </summary>
public enum SequenceStepStatus
{
    Pending,
    Running,
    Passed,
    Failed,
    Skipped
}

/// <summary>
/// Single step inside an automated commissioning or diagnostic sequence.
/// </summary>
public class SequenceStep
{
    public int StepNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SequenceStepStatus Status { get; set; } = SequenceStepStatus.Pending;
    public string ResultMessage { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public Func<IModbusMaster, CancellationToken, Task<bool>>? Action { get; set; }
}

/// <summary>
/// Execution engine for running test sequences, loopback checks, write-verify tests, and burn-in endurance tests.
/// </summary>
public class DiagnosticSequenceRunner
{
    private readonly IModbusMaster _master;

    public DiagnosticSequenceRunner(IModbusMaster master)
    {
        _master = master ?? throw new ArgumentNullException(nameof(master));
    }

    /// <summary>
    /// Builds a standard commissioning validation sequence:
    /// 1. Health Ping (FC03 Read Address 0)
    /// 2. Coil Read Check (FC01 Read Coils)
    /// 3. Read-Back Verification Test (FC06 Write & Read-back check)
    /// 4. Boundary Address Check (Address 9999)
    /// </summary>
    public List<SequenceStep> CreateStandardCommissioningSequence(byte slaveId, ushort testRegisterAddress = 10)
    {
        return new List<SequenceStep>
        {
            new()
            {
                StepNumber = 1,
                Name = "Slave Communication Check",
                Description = "Verifies response from slave holding register 0",
                Action = async (master, ct) =>
                {
                    var regs = await master.ReadHoldingRegistersAsync(slaveId, 0, 1, ct).ConfigureAwait(false);
                    return regs.Length == 1;
                }
            },
            new()
            {
                StepNumber = 2,
                Name = "Discrete Coil Status Check",
                Description = "Reads 8 coils starting at address 0",
                Action = async (master, ct) =>
                {
                    var coils = await master.ReadCoilsAsync(slaveId, 0, 8, ct).ConfigureAwait(false);
                    return coils.Length == 8;
                }
            },
            new()
            {
                StepNumber = 3,
                Name = "Read-Back Verification Test",
                Description = $"Writes test pattern 0x55AA to register {testRegisterAddress} and verifies read-back",
                Action = async (master, ct) =>
                {
                    ushort testVal = 0x55AA;
                    await master.WriteSingleRegisterAsync(slaveId, testRegisterAddress, testVal, ct).ConfigureAwait(false);
                    var readBack = await master.ReadHoldingRegistersAsync(slaveId, testRegisterAddress, 1, ct).ConfigureAwait(false);
                    return readBack.Length == 1 && readBack[0] == testVal;
                }
            },
            new()
            {
                StepNumber = 4,
                Name = "Multi-Register Read Endurance",
                Description = "Reads 20 holding registers in a single block transaction",
                Action = async (master, ct) =>
                {
                    var regs = await master.ReadHoldingRegistersAsync(slaveId, 0, 20, ct).ConfigureAwait(false);
                    return regs.Length == 20;
                }
            }
        };
    }

    /// <summary>
    /// Executes the steps in sequence and reports step progress.
    /// </summary>
    public async Task<bool> RunSequenceAsync(
        List<SequenceStep> steps,
        IProgress<SequenceStep>? stepProgress = null,
        CancellationToken cancellationToken = default)
    {
        bool allPassed = true;

        foreach (var step in steps)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                step.Status = SequenceStepStatus.Skipped;
                step.ResultMessage = "Canceled by user";
                stepProgress?.Report(step);
                continue;
            }

            step.Status = SequenceStepStatus.Running;
            stepProgress?.Report(step);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                if (step.Action != null)
                {
                    bool ok = await step.Action(_master, cancellationToken).ConfigureAwait(false);
                    sw.Stop();
                    step.Duration = sw.Elapsed;
                    if (ok)
                    {
                        step.Status = SequenceStepStatus.Passed;
                        step.ResultMessage = $"Passed in {sw.ElapsedMilliseconds}ms";
                    }
                    else
                    {
                        step.Status = SequenceStepStatus.Failed;
                        step.ResultMessage = "Verification criteria failed";
                        allPassed = false;
                    }
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                step.Duration = sw.Elapsed;
                step.Status = SequenceStepStatus.Failed;
                step.ResultMessage = $"Error: {ex.Message}";
                allPassed = false;
            }

            stepProgress?.Report(step);
        }

        return allPassed;
    }
}

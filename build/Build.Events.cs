using System.Linq;
using System.Reflection;
using Nuke.Common.Utilities.Collections;
using Nuke.Components;

partial class Build : INukeBuildEventsAware
{
    protected override void OnBuildInitialized() => InvokeEventHandlerOnComponents(nameof(OnBuildInitialized));
    protected override void OnBuildCreated() => InvokeEventHandlerOnComponents(nameof(OnBuildCreated));
    protected override void OnBuildFinished() => InvokeEventHandlerOnComponents(nameof(OnBuildFinished));
    protected override void OnTargetAbsent(string target) => InvokeEventHandlerOnComponents(nameof(OnTargetAbsent), target);
    protected override void OnTargetExecuted(string target) => InvokeEventHandlerOnComponents(nameof(OnTargetExecuted), target);

    protected override void OnTargetFailed(string target) => InvokeEventHandlerOnComponents(nameof(OnTargetFailed), target);
    protected override void OnTargetSkipped(string target) => InvokeEventHandlerOnComponents(nameof(OnTargetSkipped), target);
    protected override void OnTargetStart(string target) => InvokeEventHandlerOnComponents(nameof(OnTargetStart), target);

    void InvokeEventHandlerOnComponents(string eventName, params object[] args)
    {
        var type = typeof(INukeBuildEventsAware);
        var targetMethod = $"{type.FullName!.Replace("+", ".")}.{eventName}";
        typeof(INukeBuildEventsAware)
            .GetInterfaces()
            .Where(x =>
                x != type &&
                x.IsAssignableTo(type))
            .SelectMany(x => x.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(x => !x.IsAbstract && x.Name == targetMethod)
            .ForEach(x => x.Invoke(this, args));
    }
}
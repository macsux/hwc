using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Utilities.Collections;

namespace Nuke.Components
{
    public interface ITargetDefinitionWithInput<TIn> : ITargetDefinition
    {
        
    }

    public interface ITargetDefinitionWithOutput<TOut> : ITargetDefinition
    {
        
    }

    public interface ITargetDefinitionWithInputOutput<TIn,TOut> : ITargetDefinitionWithInput<TIn>, ITargetDefinitionWithOutput<TOut>
    {
        
    }

    public static class ArtifactIOExtensions
    {
        internal static readonly Dictionary<ITargetDefinition, object> In = new();
        internal static readonly Dictionary<ITargetDefinition, object> Out = new();
        public static ITargetDefinitionWithInput<TIn> WithInput<TIn>(this ITargetDefinition targetDefinition, Func<TIn> input)
        {
            In.Add(((TargetDefinitionWrapped)targetDefinition).Inner, input);
            return new TargetDefinitionIn<TIn>(targetDefinition);
        }
        public static ITargetDefinitionWithInputOutput<TIn,TOut> WithInput<TIn,TOut>(this ITargetDefinitionWithOutput<TOut> targetDefinition, Func<TIn> input)
        {
            In.Add(((TargetDefinitionWrapped)targetDefinition).Inner, input);
            return new TargetDefinitionIO<TIn,TOut>(targetDefinition);
        }
        public static ITargetDefinitionWithOutput<TOut> WithOutput<TOut>(this ITargetDefinition targetDefinition, Func<TOut> output)
        {
            Out.Add(((TargetDefinitionWrapped)targetDefinition).Inner, output);
            return new TargetDefinitionOut<TOut>(targetDefinition);
        }
        public static ITargetDefinitionWithInputOutput<TIn,TOut> WithOutput<TIn, TOut>(this ITargetDefinitionWithInput<TIn> targetDefinition, Func<TOut> output)
        {
            Out.Add(((TargetDefinitionWrapped)targetDefinition).Inner, output);
            return new TargetDefinitionIO<TIn, TOut>(targetDefinition);
        }
        public static ITargetDefinition Executes<TInput>(this ITargetDefinitionWithInput<TInput> targetDefinition, Action<TInput> action)
        {
        }
        public static ITargetDefinition Executes<TOutput>(this ITargetDefinitionWithOutput<TOutput> targetDefinition, Func<IEnumerable<TOutput>> action)
        {
        }
        public static ITargetDefinition Executes<TInput,TOutput>(this ITargetDefinitionWithInputOutput<TInput,TOutput> targetDefinition, Func<TInput, IEnumerable<TOutput>> action)
        {
        }

        public static ITargetDefinition ExecutesToFolder<TInput,TOutput>(this ITargetDefinitionWithInputOutput<TInput,TOutput> targetDefinition, Action<TInput, AbsolutePath> action) where TOutput : ICollection<AbsolutePath>
        {
            return targetDefinition.Executes(() =>
            {
                var temp = NukeBuild.TemporaryDirectory / Guid.NewGuid().ToString("N");
                var input = ((Func<TInput>)In[targetDefinition]).Invoke();
                action(input, temp);
                var to = ((Func<TOutput>)Out[targetDefinition]).Invoke();
                // var result = resultsList();
                
                var filesAfterTarget = temp.GlobFiles("**");
                filesAfterTarget.ForEach(file =>
                {
                    var destination = to / temp.GetRelativePathTo(file);
                    FileSystemTasks.MoveFile(file, destination, FileExistsPolicy.Overwrite);
                    result.Add(destination);
                }); 
                FileSystemTasks.DeleteDirectory(temp);
            });
        }

        public static List<AbsolutePath> AsOutput(this AbsolutePath resultDir, Action<AbsolutePath> action)
        {
            var stagingDir = NukeBuild.TemporaryDirectory / Guid.NewGuid().ToString("N");
            action(stagingDir);
            var filesAfterTarget = stagingDir.GlobFiles("**");
            var result = new List<AbsolutePath>(filesAfterTarget.Count);
            filesAfterTarget.ForEach(file =>
            {
                var destination = resultDir / stagingDir.GetRelativePathTo(file);
                FileSystemTasks.MoveFile(file, destination, FileExistsPolicy.Overwrite);
                result.Add(destination);
            });
            return result;
        }
    }

    public class TargetDefinitionIn<TIn> : TargetDefinitionWrapped, ITargetDefinitionWithInput<TIn>
    {
        public TargetDefinitionIn(ITargetDefinition inner) : base(inner)
        {
        }
    }
    public class TargetDefinitionOut<TOut> : TargetDefinitionWrapped, ITargetDefinitionWithOutput<TOut>
    {
        public TargetDefinitionOut(ITargetDefinition inner) : base(inner)
        {
        }
    }
    public class TargetDefinitionIO<TIn, TOut> : TargetDefinitionWrapped, ITargetDefinitionWithInputOutput<TIn,TOut>
    {
        public TargetDefinitionIO(ITargetDefinition inner) : base(inner)
        {
        }
    }

    public class TargetDefinitionWrapped
    {
        public ITargetDefinition Inner { get; }

        public TargetDefinitionWrapped(ITargetDefinition inner)
        {
            Inner = inner;
        }

        public ITargetDefinition Description(string description) => Inner.Description(description);

        public ITargetDefinition Executes(params Action[] actions) => Inner.Executes(actions);

        public ITargetDefinition Executes<T>(Func<T> action) => Inner.Executes(action);

        public ITargetDefinition Executes(Func<Task> action) => Inner.Executes(action);

        public ITargetDefinition DependsOn(params Target[] targets) => Inner.DependsOn(targets);

        public ITargetDefinition DependsOn<T>(params Func<T, Target>[] targets) => Inner.DependsOn(targets);

        public ITargetDefinition TryDependsOn<T>(params Func<T, Target>[] targets) => Inner.TryDependsOn(targets);

        public ITargetDefinition DependentFor(params Target[] targets) => Inner.DependentFor(targets);

        public ITargetDefinition DependentFor<T>(params Func<T, Target>[] targets) => Inner.DependentFor(targets);

        public ITargetDefinition TryDependentFor<T>(params Func<T, Target>[] targets) => Inner.TryDependentFor(targets);

        public ITargetDefinition OnlyWhenDynamic(params Expression<Func<bool>>[] conditions) => Inner.OnlyWhenDynamic(conditions);

        public ITargetDefinition OnlyWhenStatic(params Expression<Func<bool>>[] conditions) => Inner.OnlyWhenStatic(conditions);

        public ITargetDefinition Requires<T>(params Expression<Func<T>>[] parameterRequirement) where T : class => Inner.Requires(parameterRequirement);

        public ITargetDefinition Requires<T>(params Expression<Func<T?>>[] parameterRequirement) where T : struct => Inner.Requires(parameterRequirement);

        public ITargetDefinition Requires(params Expression<Func<bool>>[] requirement) => Inner.Requires(requirement);

        public ITargetDefinition WhenSkipped(DependencyBehavior dependencyBehavior) => Inner.WhenSkipped(dependencyBehavior);

        public ITargetDefinition Before(params Target[] targets) => Inner.Before(targets);

        public ITargetDefinition Before<T>(params Func<T, Target>[] targets) => Inner.Before(targets);

        public ITargetDefinition TryBefore<T>(params Func<T, Target>[] targets) => Inner.TryBefore(targets);

        public ITargetDefinition After(params Target[] targets) => Inner.After(targets);

        public ITargetDefinition After<T>(params Func<T, Target>[] targets) => Inner.After(targets);

        public ITargetDefinition TryAfter<T>(params Func<T, Target>[] targets) => Inner.TryAfter(targets);

        public ITargetDefinition Triggers(params Target[] targets) => Inner.Triggers(targets);

        public ITargetDefinition Triggers<T>(params Func<T, Target>[] targets) => Inner.Triggers(targets);

        public ITargetDefinition TryTriggers<T>(params Func<T, Target>[] targets) => Inner.TryTriggers(targets);

        public ITargetDefinition TriggeredBy(params Target[] targets) => Inner.TriggeredBy(targets);

        public ITargetDefinition TriggeredBy<T>(params Func<T, Target>[] targets) => Inner.TriggeredBy(targets);

        public ITargetDefinition TryTriggeredBy<T>(params Func<T, Target>[] targets) => Inner.TryTriggeredBy(targets);

        public ITargetDefinition AssuredAfterFailure() => Inner.AssuredAfterFailure();

        public ITargetDefinition ProceedAfterFailure() => Inner.ProceedAfterFailure();

        public ITargetDefinition Unlisted() => Inner.Unlisted();

        public ITargetDefinition Base() => Inner.Base();

        public ITargetDefinition Inherit(params Target[] targets) => Inner.Inherit(targets);

        public ITargetDefinition Inherit<T>(params Expression<Func<T, Target>>[] targets) => Inner.Inherit(targets);
    }
}
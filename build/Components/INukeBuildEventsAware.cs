namespace Nuke.Components
{
    public interface INukeBuildEventsAware
    {
        /// <summary>
        /// Method that is invoked after the instance of the build was created.
        /// </summary>
        void OnBuildCreated()
        {
        }

        /// <summary>
        /// Method that is invoked after build instance is initialized. I.e., value injection and requirement validation has finished.
        /// </summary>
        void OnBuildInitialized()
        {
        }

        /// <summary>
        /// Method that is invoked after the build has finished (succeeded or failed).
        /// </summary>
        void OnBuildFinished()
        {
        }

        /// <summary>
        /// Method that is invoked before a target is about to start.
        /// </summary>
        void OnTargetRunning(string target)
        {
        }

        /// <summary>
        /// Method that is invoked when a target is skipped.
        /// </summary>
        void OnTargetSkipped(string target)
        {
        }

        /// <summary>
        /// Method that is invoked when a target has been executed successfully. 
        /// </summary>
        void OnTargetSucceeded(string target)
        {
        }

        /// <summary>
        /// Method that is invoked when a target has failed. 
        /// </summary>
        void OnTargetFailed(string target)
        {
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Text;
using Wyam.Common.Configuration;
using Wyam.Common.Execution;
using Wyam.Common.IO;
using Wyam.Common.Modules;
using Wyam.Common.Util;

namespace Wyam.App
{
    public static class BootstrapperPipelineExtensions
    {
        // Directly

        public static IBootstrapper AddPipelines(
            this IBootstrapper boostrapper,
            Action<IPipelineCollection> action) =>
            boostrapper.Configure<IEngine>(x => action(x.Pipelines));

        public static IBootstrapper AddPipelines(
            this IBootstrapper boostrapper,
            Action<IReadOnlySettings, IPipelineCollection> action) =>
            boostrapper.Configure<IEngine>(x => action(x.Settings, x.Pipelines));

        // By type

        public static IBootstrapper AddPipeline(
            this IBootstrapper bootstrapper,
            string name,
            IPipeline pipeline) =>
            bootstrapper.Configure<IEngine>(x => x.Pipelines.Add(name, pipeline));

        public static IBootstrapper AddPipeline(
            this IBootstrapper bootstrapper,
            IPipeline pipeline) =>
            bootstrapper.Configure<IEngine>(x => x.Pipelines.Add(pipeline));

        public static IBootstrapper AddPipeline(
            this IBootstrapper bootstrapper,
            string name,
            Func<IReadOnlySettings, IPipeline> pipelineFunc) =>
            bootstrapper.Configure<IEngine>(x => x.Pipelines.Add(name, pipelineFunc(x.Settings)));

        public static IBootstrapper AddPipeline(
            this IBootstrapper bootstrapper,
            Func<IReadOnlySettings, IPipeline> pipelineFunc) =>
            bootstrapper.Configure<IEngine>(x => x.Pipelines.Add(pipelineFunc(x.Settings)));

        public static IBootstrapper AddPipeline<TPipeline>(
            this IBootstrapper bootstrapper,
            string name)
            where TPipeline : IPipeline =>
            bootstrapper.Configure<IEngine>(x => x.Pipelines.Add<TPipeline>(name));

        public static IBootstrapper AddPipeline<TPipeline>(
            this IBootstrapper bootstrapper)
            where TPipeline : IPipeline =>
            bootstrapper.Configure<IEngine>(x => x.Pipelines.Add<TPipeline>());

        // Builder

        public static IBootstrapper BuildPipeline(
            this IBootstrapper bootstrapper,
            string name,
            Action<PipelineBuilder> buildAction) =>
            bootstrapper.Configure<IEngine>(x =>
            {
                PipelineBuilder builder = new PipelineBuilder(x.Pipelines, x.Settings);
                buildAction(builder);
                IPipeline pipeline = builder.Build();
                if (pipeline != null)
                {
                    x.Pipelines.Add(name, pipeline);
                }
            });

        // Add with modules (only serial or isolated since there's no good way to specify dependencies)

        public static IBootstrapper AddSerialPipeline(
            this IBootstrapper bootstrapper,
            string name,
            IEnumerable<IModule> processModules) =>
            AddPipeline(bootstrapper, false, name, (IEnumerable<IModule>)null, processModules, null, null);

        public static IBootstrapper AddSerialPipeline(
            this IBootstrapper bootstrapper,
            string name,
            IEnumerable<IModule> readModules = null,
            IEnumerable<IModule> processModules = null,
            IEnumerable<IModule> renderModules = null,
            IEnumerable<IModule> writeModules = null) =>
            AddPipeline(bootstrapper, false, name, readModules, processModules, renderModules, writeModules);

        public static IBootstrapper AddSerialPipeline(
            this IBootstrapper bootstrapper,
            string name,
            string readPattern,
            IEnumerable<IModule> processModules = null,
            IEnumerable<IModule> renderModules = null,
            IEnumerable<IModule> writeModules = null) =>
            AddPipeline(bootstrapper, false, name, readPattern, processModules, renderModules, writeModules);

        public static IBootstrapper AddSerialPipeline(
            this IBootstrapper bootstrapper,
            string name,
            string readPattern,
            bool writeFiles,
            IEnumerable<IModule> processModules = null,
            IEnumerable<IModule> renderModules = null) =>
            AddPipeline(bootstrapper, false, name, readPattern, writeFiles, processModules, renderModules);

        public static IBootstrapper AddSerialPipeline(
            this IBootstrapper bootstrapper,
            string name,
            string readPattern,
            string writeExtension,
            IEnumerable<IModule> processModules = null,
            IEnumerable<IModule> renderModules = null) =>
            AddPipeline(bootstrapper, false, name, readPattern, writeExtension, processModules, renderModules);

        public static IBootstrapper AddSerialPipeline(
            this IBootstrapper bootstrapper,
            string name,
            string readPattern,
            DocumentConfig<FilePath> writePath,
            IEnumerable<IModule> processModules = null,
            IEnumerable<IModule> renderModules = null) =>
            AddPipeline(bootstrapper, false, name, readPattern, writePath, processModules, renderModules);

        public static IBootstrapper AddSerialPipeline(
            this IBootstrapper bootstrapper,
            string name,
            params IModule[] processModules) =>
            AddPipeline(bootstrapper, false, name, (IEnumerable<IModule>)null, processModules, null, null);

        public static IBootstrapper AddSerialPipeline(
            this IBootstrapper bootstrapper,
            string name,
            string readPattern,
            bool writeFiles,
            params IModule[] processModules) =>
            AddPipeline(bootstrapper, false, name, readPattern, writeFiles, processModules, null);

        public static IBootstrapper AddSerialPipeline(
            this IBootstrapper bootstrapper,
            string name,
            string readPattern,
            string writeExtension,
            params IModule[] processModules) =>
            AddPipeline(bootstrapper, false, name, readPattern, writeExtension, processModules, null);

        public static IBootstrapper AddSerialPipeline(
            this IBootstrapper bootstrapper,
            string name,
            string readPattern,
            DocumentConfig<FilePath> writePath,
            params IModule[] processModules) =>
            AddPipeline(bootstrapper, false, name, readPattern, writePath, processModules, null);

        public static IBootstrapper AddIsolatedPipeline(
            this IBootstrapper bootstrapper,
            string name,
            IEnumerable<IModule> processModules) =>
            AddPipeline(bootstrapper, true, name, (IEnumerable<IModule>)null, processModules, null, null);

        public static IBootstrapper AddIsolatedPipeline(
            this IBootstrapper bootstrapper,
            string name,
            IEnumerable<IModule> readModules = null,
            IEnumerable<IModule> processModules = null,
            IEnumerable<IModule> renderModules = null,
            IEnumerable<IModule> writeModules = null) =>
            AddPipeline(bootstrapper, true, name, readModules, processModules, renderModules, writeModules);

        public static IBootstrapper AddIsolatedPipeline(
            this IBootstrapper bootstrapper,
            string name,
            string readPattern,
            IEnumerable<IModule> processModules = null,
            IEnumerable<IModule> renderModules = null,
            IEnumerable<IModule> writeModules = null) =>
            AddPipeline(bootstrapper, true, name, readPattern, processModules, renderModules, writeModules);

        public static IBootstrapper AddIsolatedPipeline(
            this IBootstrapper bootstrapper,
            string name,
            string readPattern,
            bool writeFiles,
            IEnumerable<IModule> processModules = null,
            IEnumerable<IModule> renderModules = null) =>
            AddPipeline(bootstrapper, true, name, readPattern, writeFiles, processModules, renderModules);

        public static IBootstrapper AddIsolatedPipeline(
            this IBootstrapper bootstrapper,
            string name,
            string readPattern,
            string writeExtension,
            IEnumerable<IModule> processModules = null,
            IEnumerable<IModule> renderModules = null) =>
            AddPipeline(bootstrapper, true, name, readPattern, writeExtension, processModules, renderModules);

        public static IBootstrapper AddIsolatedPipeline(
            this IBootstrapper bootstrapper,
            string name,
            string readPattern,
            DocumentConfig<FilePath> writePath,
            IEnumerable<IModule> processModules = null,
            IEnumerable<IModule> renderModules = null) =>
            AddPipeline(bootstrapper, true, name, readPattern, writePath, processModules, renderModules);

        public static IBootstrapper AddIsolatedPipeline(
            this IBootstrapper bootstrapper,
            string name,
            params IModule[] processModules) =>
            AddPipeline(bootstrapper, true, name, (IEnumerable<IModule>)null, processModules, null, null);

        public static IBootstrapper AddIsolatedPipeline(
            this IBootstrapper bootstrapper,
            string name,
            string readPattern,
            bool writeFiles,
            params IModule[] processModules) =>
            AddPipeline(bootstrapper, true, name, readPattern, writeFiles, processModules, null);

        public static IBootstrapper AddIsolatedPipeline(
            this IBootstrapper bootstrapper,
            string name,
            string readPattern,
            string writeExtension,
            params IModule[] processModules) =>
            AddPipeline(bootstrapper, true, name, readPattern, writeExtension, processModules, null);

        public static IBootstrapper AddIsolatedPipeline(
            this IBootstrapper bootstrapper,
            string name,
            string readPattern,
            DocumentConfig<FilePath> writePath,
            params IModule[] processModules) =>
            AddPipeline(bootstrapper, true, name, readPattern, writePath, processModules, null);

        // Helpers for adding serial or isolated pipelines

        private static IBootstrapper AddPipeline(
            IBootstrapper bootstrapper,
            bool isolated,
            string name,
            IEnumerable<IModule> readModules,
            IEnumerable<IModule> processModules,
            IEnumerable<IModule> renderModules,
            IEnumerable<IModule> writeModules) =>
            bootstrapper.BuildPipeline(name, builder =>
            {
                builder
                    .AsIsolatedOrStatic(isolated)
                    .WithReadModules(readModules)
                    .WithProcessModules(processModules)
                    .WithRenderModules(renderModules)
                    .WithWriteModules(writeModules);
            });

        private static IBootstrapper AddPipeline(
            IBootstrapper bootstrapper,
            bool isolated,
            string name,
            string readPattern,
            IEnumerable<IModule> processModules,
            IEnumerable<IModule> renderModules,
            IEnumerable<IModule> writeModules) =>
            bootstrapper.BuildPipeline(name, builder =>
            {
                builder
                    .AsIsolatedOrStatic(isolated)
                    .WithReadFiles(readPattern)
                    .WithProcessModules(processModules)
                    .WithRenderModules(renderModules)
                    .WithWriteModules(writeModules);
            });

        private static IBootstrapper AddPipeline(
            IBootstrapper bootstrapper,
            bool isolated,
            string name,
            string readPattern,
            bool writeFiles,
            IEnumerable<IModule> processModules,
            IEnumerable<IModule> renderModules) =>
            bootstrapper.BuildPipeline(name, builder =>
            {
                builder
                    .AsIsolatedOrStatic(isolated)
                    .WithReadFiles(readPattern)
                    .WithProcessModules(processModules)
                    .WithRenderModules(renderModules);
                if (writeFiles)
                {
                    builder.WithWriteModules();
                }
            });

        private static IBootstrapper AddPipeline(
            IBootstrapper bootstrapper,
            bool isolated,
            string name,
            string readPattern,
            string writeExtension,
            IEnumerable<IModule> processModules,
            IEnumerable<IModule> renderModules) =>
            bootstrapper.BuildPipeline(name, builder =>
            {
                builder
                    .AsIsolatedOrStatic(isolated)
                    .WithReadFiles(readPattern)
                    .WithProcessModules(processModules)
                    .WithRenderModules(renderModules)
                    .WithWriteFiles(writeExtension);
            });

        private static IBootstrapper AddPipeline(
            IBootstrapper bootstrapper,
            bool isolated,
            string name,
            string readPattern,
            DocumentConfig<FilePath> writePath,
            IEnumerable<IModule> processModules,
            IEnumerable<IModule> renderModules) =>
            bootstrapper.BuildPipeline(name, builder =>
            {
                builder
                    .AsIsolatedOrStatic(isolated)
                    .WithReadFiles(readPattern)
                    .WithProcessModules(processModules)
                    .WithRenderModules(renderModules)
                    .WithWriteFiles(writePath);
            });

        private static PipelineBuilder AsIsolatedOrStatic(this PipelineBuilder builder, bool isolated)
        {
            if (isolated)
            {
                builder.AsIsolated();
            }
            else
            {
                builder.AsSerial();
            }
            return builder;
        }
    }
}
﻿using System.Reflection;
using Ardalis.SharedKernel;
using Autofac;
using Nimble.GuestbookApp.Core.ContributorAggregate;
using Nimble.GuestbookApp.Core.Interfaces;
using Nimble.GuestbookApp.Infrastructure.Data;
using Nimble.GuestbookApp.Infrastructure.Data.Queries;
using Nimble.GuestbookApp.Infrastructure.Email;
using Nimble.GuestbookApp.UseCases.Contributors.Create;
using Nimble.GuestbookApp.UseCases.Contributors.List;
using MediatR;
using MediatR.Pipeline;
using Module = Autofac.Module;
using Nimble.GuestbookApp.UseCases.Entries;
using Dapper;

namespace Nimble.GuestbookApp.Infrastructure;

/// <summary>
/// An Autofac module responsible for wiring up services defined in Infrastructure.
/// Mainly responsible for setting up EF and MediatR, as well as other one-off services.
/// </summary>
public class AutofacInfrastructureModule : Module
{
  private readonly bool _isDevelopment = false;
  private readonly List<Assembly> _assemblies = new List<Assembly>();

  public AutofacInfrastructureModule(bool isDevelopment, Assembly? callingAssembly = null)
  {
    _isDevelopment = isDevelopment;
    AddToAssembliesIfNotNull(callingAssembly);
  }

  private void AddToAssembliesIfNotNull(Assembly? assembly)
  {
    if (assembly != null)
    {
      _assemblies.Add(assembly);
    }
  }

  private void LoadAssemblies()
  {
    // TODO: Replace these types with any type in the appropriate assembly/project
    var coreAssembly = Assembly.GetAssembly(typeof(Contributor));
    var infrastructureAssembly = Assembly.GetAssembly(typeof(AutofacInfrastructureModule));
    var useCasesAssembly = Assembly.GetAssembly(typeof(CreateContributorCommand));

    AddToAssembliesIfNotNull(coreAssembly);
    AddToAssembliesIfNotNull(infrastructureAssembly);
    AddToAssembliesIfNotNull(useCasesAssembly);
  }

  protected override void Load(ContainerBuilder builder)
  {
    LoadAssemblies();
    if (_isDevelopment && false)
    {
      RegisterDevelopmentOnlyDependencies(builder);
    }
    else
    {
      RegisterProductionOnlyDependencies(builder);
    }
    RegisterEF(builder);

    builder.RegisterType<DapperContext>()
      .SingleInstance();

    RegisterQueries(builder);
    RegisterMediatR(builder);
  }

  private void RegisterEF(ContainerBuilder builder)
  {
    builder.RegisterGeneric(typeof(EfRepository<>))
      .As(typeof(IRepository<>))
      .As(typeof(IReadRepository<>))
      .InstancePerLifetimeScope();
  }

  private void RegisterQueries(ContainerBuilder builder)
  {
    // builder.RegisterType<FakeListEntriesQueryService>()
    //   .As<IListEntriesQueryService>()
    //   .InstancePerLifetimeScope();

    // builder.RegisterType<ListEntriesDapperQueryService>()
    //   .As<IListEntriesQueryService>()
    //   .InstancePerLifetimeScope();
    builder.RegisterType<ListEntriesQueryService>()
      .As<IListEntriesQueryService>()
      .InstancePerLifetimeScope();
  }

  private void RegisterMediatR(ContainerBuilder builder)
  {
    builder
      .RegisterType<Mediator>()
      .As<IMediator>()
      .InstancePerLifetimeScope();

    builder
      .RegisterGeneric(typeof(LoggingBehavior<,>))
      .As(typeof(IPipelineBehavior<,>))
      .InstancePerLifetimeScope();

    builder
      .RegisterType<MediatRDomainEventDispatcher>()
      .As<IDomainEventDispatcher>()
      .InstancePerLifetimeScope();

    var mediatrOpenTypes = new[]
    {
      typeof(IRequestHandler<,>),
      typeof(IRequestExceptionHandler<,,>),
      typeof(IRequestExceptionAction<,>),
      typeof(INotificationHandler<>),
    };

    foreach (var mediatrOpenType in mediatrOpenTypes)
    {
      builder
        .RegisterAssemblyTypes(_assemblies.ToArray())
        .AsClosedTypesOf(mediatrOpenType)
        .AsImplementedInterfaces();
    }
  }

  private void RegisterDevelopmentOnlyDependencies(ContainerBuilder builder)
  {
    // NOTE: Add any development only services here
    builder.RegisterType<FakeEmailSender>().As<IEmailSender>()
      .InstancePerLifetimeScope();

    builder.RegisterType<FakeListContributorsQueryService>()
      .As<IListContributorsQueryService>()
      .InstancePerLifetimeScope();

  }

  private void RegisterProductionOnlyDependencies(ContainerBuilder builder)
  {
    // NOTE: Add any production only (real) services here
    builder.RegisterType<SmtpEmailSender>().As<IEmailSender>()
      .InstancePerLifetimeScope();

    builder.RegisterType<ListContributorsQueryService>()
      .As<IListContributorsQueryService>()
      .InstancePerLifetimeScope();

  }
}

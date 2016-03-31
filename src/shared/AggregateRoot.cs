﻿using System;
using System.Collections.Generic;

namespace shared
{
	public abstract class AggregateRoot : IEventProducer, IEventConsumer<Event>, IEquatable<IIdentity>
	{
		private readonly List<Event> _events = new List<Event>();

		protected AggregateRoot(Guid id)
		{
			Id = id;
		}

		public Guid Id { get; }

		public int Version { get; private set; } = -1;

		protected void RecordThat<TEvent>(TEvent @event) where TEvent : Event<TEvent>
		{
			_events.Add(@event);
			Apply(@event);
		}

		public IReadOnlyList<Event> Events => _events;

		public virtual void Apply(Event @event)
		{
			Version++;
		}

		public bool Equals(IIdentity other)
		{
			return other != null && other.GetType() == GetType() && other.Id == Id;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as IIdentity);
		}

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}
	}

	public abstract class AggregateRoot<TState> : AggregateRoot where TState : IEventConsumer, new()
	{
		protected readonly TState State = new TState();

		protected AggregateRoot(Guid id) : base(id)
		{

		}

		public sealed override void Apply(Event @event)
		{
			@event.ApplyTo(State);
			base.Apply(@event);
		}
	}

}

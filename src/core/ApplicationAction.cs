﻿using System;
using System.Collections.Generic;
using shared;

namespace core
{
	public static class ApplicationAction
	{
		public static IEnumerable<IEvent> Start()
		{
			yield return new ApplicationStarted();
		}

		public static IEnumerable<IEvent> Submit(WhenSubmittingApplicationState state, string submittedBy)
		{
			Ensure.NotNull(state, nameof(state));
			if (!state.HasBeenStarted)
			{
				throw new Exception("application has not been started");
			}
			if (state.HasBeenSubmitted)
			{
				throw new Exception("application has already been submitted");
			}
			yield return new ApplicationSubmitted(submittedBy);
		}
	}
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WsFSM
{
	public delegate void WsStateDelegate();
	public delegate void WsStateDelegateFloat(float f);
	public delegate void WsateDelegateState(IState state);

	public class WsState : IState
	{

		public string Name
		{
			get
			{
				return _name;
			}
		}

		public string Tag
		{
			get { return _tag; }
			set { _tag = value; }
		}

		public float Timer
		{
			get { return _timer; }
		}

		public List<ITransition> Transitions
		{
			get { return _transitions; }
		}

		public IStateMachine Parent
		{
			get
			{
				return _parent;
			}

			set
			{
				_parent = value;
			}
		}

		public event WsateDelegateState OnEnter;
		public event WsateDelegateState OnExit;
		public event WsStateDelegateFloat OnUpdate;
		public event WsStateDelegateFloat OnLateUpdate;
		public event WsStateDelegate OnFixedUpdate;

		public WsState(string name)
		{
			_name = name;
			_transitions = new List<ITransition>();
		}
        public float ResetTimer(float t)
        {
            _timer = t;
            return _timer;
        }
		public void AddTransition(ITransition t)
		{
			if (t != null && !_transitions.Contains(t))
			{
				_transitions.Add(t);
			}
		}

		public virtual void EnterCallback(IState prev)
		{
			_timer = 0f;
			OnEnter?.Invoke(prev);
		}

		public virtual void ExitCallback(IState next)
		{
            OnExit?.Invoke(next);
            _timer = 0f;
		}

		public virtual void FixedUpdateCallback()
		{
            OnFixedUpdate?.Invoke();
        }

		public virtual void LateUpdateCallback(float deltaTime)
		{
            OnLateUpdate?.Invoke(deltaTime);
        }

		public virtual void UpdateCallback(float deltaTime)
		{
            OnUpdate?.Invoke(deltaTime);
            _timer += deltaTime;
		}

		private string _name;
		private string _tag;
		private List<ITransition> _transitions;
		private float _timer;
		private IStateMachine _parent;
	}
}

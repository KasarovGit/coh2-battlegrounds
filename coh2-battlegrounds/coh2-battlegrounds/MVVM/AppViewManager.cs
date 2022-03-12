﻿using System;
using System.Collections.Generic;

using BattlegroundsApp.Modals;

using ViewModelType = Battlegrounds.Functional.Either<string, System.Type, BattlegroundsApp.MVVM.IViewModel>;

namespace BattlegroundsApp.MVVM {

    public enum AppDisplayState {
        LeftRight,
        Full
    }

    public enum AppDisplayTarget {
        Left,
        Right,
        Full
    }

    public class AppViewManager {

        private readonly Dictionary<string, IViewModel> m_models;
        private readonly MainWindow m_window;

        public AppViewManager(MainWindow window) {
            this.m_models = new();
            this.m_window = window;
        }

        public void SetDisplay(AppDisplayState displayState, ViewModelType left, ViewModelType right) {
            if (displayState == AppDisplayState.LeftRight) {
                this.m_window.SetLeftPanel(this.SolveDisplay(left));
                this.m_window.SetRightPanel(this.SolveDisplay(right));
            } else if (displayState == AppDisplayState.Full) {
                this.m_window.SetFull(this.SolveDisplay(left) ?? this.SolveDisplay(right)); // Use left, and right if left was invalid.
            } else {
                throw new NotImplementedException();
            }
        }

        public IViewModel? SolveDisplay(ViewModelType view) {
            if (view.IfFirstOption(out string str)) {
                return this.m_models.GetValueOrDefault(str, null);
            } else if (view.IfSecondOption(out var t)) {
                return this.m_models.GetValueOrDefault(t.Name, null);
            } else if (view.IfThirdOption(out var mdl)) {
                string tyKey = mdl.GetType().Name;
                if (mdl.SingleInstanceOnly) {
                    if (this.m_models.TryGetValue(tyKey, out var model)) {
                        return model;
                    }
                    return this.m_models[tyKey] = mdl;
                } else {
                    if (this.m_models.TryGetValue(tyKey, out var model) && !model.UnloadViewModel()) {
                        return model;
                    }
                    return this.m_models[tyKey] = mdl;
                }
            }
            return null;
        }

        public void UpdateDisplay(AppDisplayTarget target, ViewModelType display) {
            var model = this.SolveDisplay(display);
            if (!this.UpdateDisplay(target, model)) {
                throw new InvalidOperationException();
            }
        }

        public T UpdateDisplay<T>(AppDisplayTarget target) where T : IViewModel {
            var model = this.SolveDisplay(typeof(T));
            return this.UpdateDisplay(target, model) ? (T)model : throw new InvalidOperationException();
        }

        private bool UpdateDisplay(AppDisplayTarget target, IViewModel model) {
            if (target == AppDisplayTarget.Full && this.m_window.DisplayState != AppDisplayState.Full) {
                throw new InvalidOperationException();
            }
            switch (target) {
                case AppDisplayTarget.Left:
                    this.m_window.SetLeftPanel(model);
                    break;
                case AppDisplayTarget.Right:
                    this.m_window.SetRightPanel(model);
                    break;
                case AppDisplayTarget.Full:
                    this.m_window.SetFull(model);
                    break;
                default:
                    return false;
            }
            return true;
        }

        public T? CreateDisplayIfNotFound<T>(Func<T> creator) where T : class, IViewModel
            => (this.m_models.TryGetValue(typeof(T).Name, out var model) ? model : (this.m_models[typeof(T).Name] = creator())) as T;

        public bool CloseDisplay<T>()
            => this.m_models.TryGetValue(typeof(T).Name, out var model) && model.UnloadViewModel();

        public ModalControl? GetModalControl() 
            => this.m_window.ModalView;

    }

}

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.ViewModels
{
    public class CreateWorkoutViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<MuscleGroup> MuscleGroups { get; } = new (Enum.GetValues<MuscleGroup>());

        private string workoutName_ = string.Empty;
        public string WorkoutName
        {
            get => workoutName_;
            set
            {
                workoutName_ = value;
                OnPropertyChanged(nameof(WorkoutName));
            }
        }

        public ObservableCollection<TemplateExercise> Exercises { get; } = new ObservableCollection<TemplateExercise>();
        public ObservableCollection<string> AvailableExercises { get; } = new ObservableCollection<string>();

        private string? selectedNewExercise_;
        public string? SelectedNewExercise
        {
            get => selectedNewExercise_;
            set
            {
                selectedNewExercise_ = value;
                OnPropertyChanged(nameof(SelectedNewExercise));
            }
        }

        private double newExerciseSets_ = 3;
        public double NewExerciseSets
        {
            get => newExerciseSets_;
            set
            {
                newExerciseSets_ = value;
                OnPropertyChanged(nameof(NewExerciseSets));
            }
        }

        private double newExerciseReps_ = 10;
        public double NewExerciseReps
        {
            get => newExerciseReps_;
            set
            {
                newExerciseReps_ = value;
                OnPropertyChanged(nameof(NewExerciseReps));
            }
        }

        // Commands
        public ICommand AddExerciseCommand { get; }
        public ICommand RemoveExerciseCommand { get; }
        public ICommand SaveWorkoutCommand { get; }

        private readonly IDataStorage dataStorage_;

        public int ClientId { get; set; }

        public event Action? WorkoutSaved;

        public CreateWorkoutViewModel(IDataStorage dataStorage)
        {
            dataStorage_ = dataStorage;

            AddExerciseCommand = new RelayCommand(AddExercise);
            RemoveExerciseCommand = new RelayCommand<TemplateExercise>(RemoveExercise);
            SaveWorkoutCommand = new RelayCommand(SaveWorkout);

            LoadAvailableExercises();
        }

        private void AddExercise()
        {
            if (string.IsNullOrWhiteSpace(SelectedNewExercise))
            {
                return;
            }

            Exercises.Add(new TemplateExercise
            {
                Name = SelectedNewExercise,
                TargetSets = (int)NewExerciseSets,
                TargetReps = (int)NewExerciseReps,
                MuscleGroup = MuscleGroup.OTHER
            });

            SelectedNewExercise = null;
        }

        private void RemoveExercise(TemplateExercise? exercise)
        {
            if (exercise == null)
            {
                return;
            }
            Exercises.Remove(exercise);
        }

        private void SaveWorkout()
        {
            if (string.IsNullOrWhiteSpace(WorkoutName) || Exercises.Count == 0)
            {
                return;
            }

            var newWorkout = new WorkoutTemplate
            {
                Name = WorkoutName,
                Type = WorkoutType.CUSTOM,
                ClientId = ClientId
            };

            foreach (var exercise in Exercises)
            {
                newWorkout.AddExercise(exercise);
            }

            dataStorage_.SaveTrainerWorkout(newWorkout);
            WorkoutSaved?.Invoke();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void LoadAvailableExercises()
        {
            AvailableExercises.Clear();

            foreach (var name in dataStorage_.GetAllExerciseNames())
            {
                AvailableExercises.Add(name);
            }
        }
    }
}

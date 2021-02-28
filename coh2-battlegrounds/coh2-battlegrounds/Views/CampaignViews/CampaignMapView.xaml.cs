﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Battlegrounds.Campaigns;
using Battlegrounds.Campaigns.Controller;
using Battlegrounds.Campaigns.Organisations;
using Battlegrounds.Functional;
using Battlegrounds.Gfx;

using BattlegroundsApp.Resources;
using BattlegroundsApp.Views.CampaignViews.Models;

namespace BattlegroundsApp.Views.CampaignViews {
    
    /// <summary>
    /// Interaction logic for CampaignMapView.xaml
    /// </summary>
    public partial class CampaignMapView : ViewState, INotifyPropertyChanged {

        private List<CampaignUnitFormationModel> m_formationViews;
        private Dictionary<string, ImageSource> m_graphics;

        public CampaignUnitSelectionModel Selection { get; }

        public ICampaignController Controller { get; }

        public ImageSource CampaignMapImage { get; }

        public double CampaignMapWidth => this.CampaignMapImage.Width;

        public double CampaignMapHeight => this.CampaignMapImage.Height;

        public string CampaignDate => this.Controller.Campaign.Turn.Date;

        public event PropertyChangedEventHandler PropertyChanged;

        public CampaignMapView(ICampaignController controller) {

            // Assign controller
            this.Controller = controller;

            // Init list
            this.m_formationViews = new List<CampaignUnitFormationModel>();

            // Load graphics
            this.InitializeGrapihcs();

            // Init components
            this.InitializeComponent();

            // Init data
            this.Selection = new CampaignUnitSelectionModel();
            this.CampaignMapImage = PngImageSource.FromMemory(controller.Campaign.PlayMap.RawImageData);
            this.CreateNodeNetwork();
            this.RefreshDisplayedFormations();

            // Hide chat control if singleplayer
            if (controller.IsSingleplayer) {
                this.CampaignChat.Visibility = Visibility.Collapsed;
                // TODO: Expand selection view
            } else {

            }

        }

        private void InitializeGrapihcs() {

            // Create graphics dictionary
            this.m_graphics = new Dictionary<string, ImageSource>();

            // For each GFX map
            this.Controller.Campaign.GfxMaps.ForEach(x => {

                // Loop over all resources
                x.Resources.ForEach(id => {

                    // Get the resource
                    GfxResource resource = x.GetResource(id);
                    var stream = resource.Open();

                    // Decode and store the image
                    this.m_graphics[id] = new PngBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.Default).Frames[0];

                });

            });

        }

        public override void StateOnFocus() {}

        public override void StateOnLostFocus() {}

        private void CreateNodeNetwork() {

            // Loop through all nodes
            this.Controller.Campaign.PlayMap.EachNode(n => {

                // Create figure and add to canvas
                Ellipse node = new Ellipse {
                    Width = 32,
                    Height = 32,
                    Fill = n.Owner == CampaignArmyTeam.TEAM_ALLIES ? Brushes.Red : (n.Owner == CampaignArmyTeam.TEAM_AXIS ? Brushes.Green : Brushes.Gray),
                    Tag = n,
                };

                // Add events
                node.MouseDown += this.NodeClicked;

                // Add node
                this.CampaignMapCanvas.Children.Add(node);

                // Set visual
                n.VisualNode = node;

                // Set position of node
                SetPosition(node, n.U * this.CampaignMapWidth - (32.0 / 2.0), n.V * this.CampaignMapHeight - (32.0 / 2.0));

            });

            // Loop through all transitions
            this.Controller.Campaign.PlayMap.EachTransition(t => {

                // Create line object
                Line line = new Line {
                    X1 = t.From.U * this.CampaignMapWidth,
                    Y1 = t.From.V * this.CampaignMapHeight,
                    X2 = t.To.U * this.CampaignMapWidth,
                    Y2 = t.To.V * this.CampaignMapHeight,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1.75,
                    Tag = t
                };

                // Add line
                this.CampaignMapCanvas.Children.Add(line);

                // Set Z-index
                line.SetValue(Panel.ZIndexProperty, 99);

            });

        }

        private void RefreshDisplayedFormations() {

            // Clear all formations
            this.ClearFormations();

            // Loop through all formations and display them
            this.Controller.Campaign.PlayMap.EachFormation(f => {

                // Banner file
                string banner_element = $"{f.Army}_banner";

                // Create space for UI element (default to null)
                UIElement displayElement = null;

                // If it exists, use an image, otherwise some other default pinkish shape
                if (this.m_graphics.TryGetValue(banner_element, out ImageSource img)) {

                    // Create image
                    displayElement = new Image() {
                        Source = img,
                        Width = 24,
                        Height = 32,
                        Tag = f,
                    };

                    // Add handler(s)
                    displayElement.MouseDown += this.BannerClicked;

                    // Add to canvas
                    this.CampaignMapCanvas.Children.Add(displayElement);

                    // Set the display position
                    SetPosition(displayElement, f.Node.U * this.CampaignMapWidth, f.Node.V * this.CampaignMapHeight);

                }

                // Create container type
                CampaignUnitFormationModel cufv = new CampaignUnitFormationModel(displayElement, f);

                this.m_formationViews.Add(cufv);

            });

        }

        private void ClearFormations() {
            foreach (var form in this.m_formationViews) {
                if (form.Element is not null) {
                    this.CampaignMapCanvas.Children.Remove(form.Element);
                }
            }
            this.m_formationViews.Clear();
        }

        private void NodeClicked(object sender, MouseButtonEventArgs e) {
            if (sender is Ellipse ellipse && ellipse.Tag is CampaignMapNode node) {
                if (e.LeftButton == MouseButtonState.Pressed) {
                    this.Selection.Select(node.Occupants.Select(x => this.FromFormation(x)));
                } else if (e.RightButton == MouseButtonState.Pressed) {
                    if (this.Selection.Size > 0) {
                        if (this.Selection.Shares(x => x.Formation.Node)) {
                            this.Selection.InvokeEach(x => {

                                var path = this.Controller.Campaign.PlayMap.FindPath(x.Formation.Node, node);

                                Trace.WriteLine(string.Join(" -> ", path.Select(x => x.NodeName)));

                            });
                        }
                    }
                }
            }
        }

        private void BannerClicked(object sender, MouseButtonEventArgs e) {
            if (sender is Image img && img.Tag is Formation formation) {
                if (this.FromFormation(formation) is CampaignUnitFormationModel model) {
                    if (e.LeftButton == MouseButtonState.Pressed) {
                        Keyboard.IsKeyDown(Key.LeftShift).IfTrue().Then(() => this.Selection.AddToSelection(model)).Else(() => this.Selection.Select(model));
                    }
                }
            }
        }

        private CampaignUnitFormationModel FromFormation(Formation formation) => this.m_formationViews.FirstOrDefault(x => x.Formation == formation);

        private static void SetPosition(UIElement element, double x, double y) {
            element.SetValue(Canvas.LeftProperty, x);
            element.SetValue(Canvas.TopProperty, y);
            element.SetValue(Panel.ZIndexProperty, 100);
        }

        private void EndTurnBttn_Click(object sender, RoutedEventArgs e) {
            if (!this.Controller.EndTurn()) {
                this.Controller.EndCampaign();
            }
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.CampaignDate)));
        }

        private void LeaveAndSaveButton_Click(object sender, RoutedEventArgs e) {

        }

    }

}

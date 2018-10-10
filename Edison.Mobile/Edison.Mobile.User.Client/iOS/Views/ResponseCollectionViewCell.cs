﻿using System;
using System.Threading.Tasks;
using CoreGraphics;
using CoreLocation;
using Edison.Mobile.iOS.Common.Shared;
using Edison.Mobile.iOS.Common.Views;
using Edison.Mobile.User.Client.Core.CollectionItemViewModels;
using Edison.Mobile.User.Client.iOS.Shared;

namespace Edison.Mobile.User.Client.iOS.Views
{
    public class ResponseCollectionViewCell : BaseCollectionViewCell<ResponseCollectionItemViewModel>
    {
        ResponseDetailsView detailsView;
        ResponseMapView mapView;
        Guid? previousResponseId;

        public ResponseCollectionViewCell(IntPtr handle) : base(handle) { }

        public void Initialize()
        {
            if (!isInitialized)
            {
                Layer.ShadowColor = PlatformConstants.Color.Black.CGColor;
                Layer.ShadowOffset = CGSize.Empty;
                Layer.ShadowOpacity = 0.45f;
                Layer.ShadowRadius = 8;
                Layer.CornerRadius = Constants.CornerRadius;

                ContentView.Layer.MasksToBounds = true;
                ContentView.Layer.CornerRadius = Constants.CornerRadius;

                detailsView = new ResponseDetailsView
                {
                    TranslatesAutoresizingMaskIntoConstraints = false,
                };

                ContentView.AddSubview(detailsView);

                detailsView.LeftAnchor.ConstraintEqualTo(ContentView.LeftAnchor).Active = true;
                detailsView.RightAnchor.ConstraintEqualTo(ContentView.RightAnchor).Active = true;
                detailsView.BottomAnchor.ConstraintEqualTo(ContentView.BottomAnchor).Active = true;
                detailsView.HeightAnchor.ConstraintEqualTo(ContentView.HeightAnchor, multiplier: 2f / 3f).Active = true;

                mapView = new ResponseMapView { TranslatesAutoresizingMaskIntoConstraints = false };

                ContentView.AddSubview(mapView);

                mapView.LeftAnchor.ConstraintEqualTo(ContentView.LeftAnchor).Active = true;
                mapView.RightAnchor.ConstraintEqualTo(ContentView.RightAnchor).Active = true;
                mapView.TopAnchor.ConstraintEqualTo(ContentView.TopAnchor).Active = true;
                mapView.BottomAnchor.ConstraintEqualTo(detailsView.TopAnchor).Active = true;

                isInitialized = true;
            }

            mapView.EventLocation = ViewModel?.Geolocation != null ? new CLLocation(ViewModel.Geolocation.Latitude, ViewModel.Geolocation.Longitude) : null;

            if (ViewModel?.ResponseId != previousResponseId)
            {
                Task.Run(async () => await ViewModel?.GetResponse());
            }

            previousResponseId = ViewModel?.ResponseId;
        }

        public override void BindEventHandlers()
        {
            base.BindEventHandlers();

            ViewModel.OnResponseReceived += HandleOnResponseReceived;
            ViewModel.OnLocationChanged += HandleOnLocationChanged;
        }

        public override void UnbindEventHandlers()
        {
            base.UnbindEventHandlers();

            ViewModel.OnResponseReceived -= HandleOnResponseReceived;
            ViewModel.OnLocationChanged -= HandleOnLocationChanged;
        }

        void HandleOnResponseReceived()
        {
            if (ViewModel.Response == null) 
            {
                return;
            }

            InvokeOnMainThread(() =>
            {
                mapView.EventLocation = new CLLocation(ViewModel.Response.Geolocation.Latitude, ViewModel.Response.Geolocation.Longitude);
                detailsView.ActionPlan = ViewModel.Response.ActionPlan;
            });
        }

        void HandleOnLocationChanged(object sender, Common.Geolocation.LocationChangedEventArgs e)
        {
            mapView.UserLocation = new CLLocation(e.CurrentLocation.Latitude, e.CurrentLocation.Longitude);
        }
    }
}

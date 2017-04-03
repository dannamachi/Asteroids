﻿using System;
using SFML.System;
using SFML.Window;
using SFML.Graphics;
using System.Collections.Generic;

namespace Asteroids
{
    /// <summary>
    /// Class for the ship that the player will be controlling.
    /// All ships are initialized with zero velocity
    /// Args:
    ///     p(Vector2f) : the position that the ship is starting at
    ///     sideLength(float) : The side length of the triangle that makes up the ship
    /// </summary> 
    class Ship : Entity
    {
        // For out of bounds checks
        private float shipLength;

        // Has the user rotated, thrust, or shot recently
        private bool hasThrust = false;
        private bool hasSpin = false;

        // More specific shooting variables
        private bool isShotCharged = false;
        private bool wantsToShoot = false; // This variable is checked in the game loop
        private Byte shotCounter;
        private const Byte shotChargingTime = 5; //Amount of frames to be wait before shot is charged

        // How much to increase velocity by when key is pressed
        private const float rotationPower = 30;
        private const float thrustPower = 30;

        // Keeping the spaceship velocities reasonable
        private const float terminalVelocitySquared = 300*300; // To avoid SQRT
        private const float decayRate = .9f;
        private const float angularDecayRate = .7f;

        // Note that shape.Rotation acts as angular position
        private float angularVelocity;

        // For drawing thrust jet
        private Shape jetShape;
        private const float THRUSTER_ROTATION_OFFSET = 180;

        public Ship(Vector2f p, float sideLength)
        {
            // Drawing ship
            shipLength = sideLength;
            shape = new CircleShape(sideLength, 3); // CircleShape(~,3) creates a triangle
            shape.Scale = new Vector2f(0.7f, 1); // Make the ship longer than it is wide
            // Set the center of the shape for convienience later
            shape.Origin = new Vector2f(sideLength, sideLength);
            shape.Position = p;
            velocity = new Vector2f(0, 0);

            shape.FillColor = Color.Black;
            shape.OutlineColor = Color.White;
            shape.OutlineThickness = -3;

            // Setting thrust triangle characteristics
            jetShape = new CircleShape(sideLength / 2, 3);
            jetShape.Scale = new Vector2f(0.6f, 1);
            jetShape.Origin = new Vector2f(sideLength / 2, sideLength / 1.5f);
            jetShape.Rotation = shape.Rotation + THRUSTER_ROTATION_OFFSET;
            jetShape.Position = p;
            jetShape.FillColor = Color.Cyan;

        }
        /// <summary>
        /// Draws the ship and the jet, if the user has recently
        /// pressed the thrust key
        /// </summary>
        /// <param name="window"></param>
        public override void Draw(RenderWindow window)
        {
            Edge curEdge = OutOfBoundsEdge(window, shipLength/2);
            if (curEdge != Edge.NULL) ResetPosition(curEdge, window, shipLength/2);
            // Because keyboard repeating rate < frame rate , you get flickering of jet
            // Possibly implement a counter to fix this? (Not that important...)
            if (hasThrust) window.Draw(jetShape);
            window.Draw(shape);

            // Reset these here so I know when to draw jet
            hasThrust = false;
            hasSpin = false;
        }
        

        /// <summary>
        /// The ship update function applies thrust & rotation if applicable
        /// (User has pressed the appriopiate keys) and then applies the 
        /// kinematic equations to change the ships position
        /// </summary>
        /// <param name="dt">The elapsed time, used in kinematic equations</param>
        public override void Update(float dt)
        {
            // Thrust controls
            if (Keyboard.IsKeyPressed(Keyboard.Key.Up)) Thrust(-1);

            // Rotation controls
            if (Keyboard.IsKeyPressed(Keyboard.Key.Right)) Rotate(1);
            else if (Keyboard.IsKeyPressed(Keyboard.Key.Left)) Rotate(-1);
            // Shooting controls
            if (Keyboard.IsKeyPressed(Keyboard.Key.Space) && isShotCharged) wantsToShoot = true;
            else ChargeShot();
            // Position updates
            Kinematics(dt);
            // Thrust Position updates
            jetShape.Position = GetThrusterPostion();
            jetShape.Rotation = shape.Rotation + THRUSTER_ROTATION_OFFSET;
        }
        /// <summary>
        /// Applies a thrust in the direction that the ship
        /// is currently facing
        /// </summary>
        /// <param name="direction">
        /// Dictates whether the ship moves backward or forward 
        /// </param>
        public void Thrust(sbyte direction)
        {
            // NOTE: The reason for the orientation of sin and cos is because shape.Rotation is 0 deg at "90 degrees"
            // Essentially phase shifted by 90 degrees - SUJECT TO CHANGE
            float headingRads = shape.Rotation.degToRads();
            float xComponent = (float) Math.Sin(headingRads) * thrustPower * direction;
            float yComponent = (float) Math.Cos(headingRads) * thrustPower * direction;

            if (velocity.MagnitudeSquared() < terminalVelocitySquared)
            {
                velocity = new Vector2f(velocity.X - xComponent, velocity.Y + yComponent);
                hasThrust = true;
            }
        }
        public void Rotate(sbyte direction)
        {
            angularVelocity += rotationPower * direction;
            hasSpin = true;
        }
        /// <summary>
        /// Applies toy kinematic equations to determine current
        /// position and heading of the ship
        /// </summary>
        /// <param name="dt">A small timestep based on framerate</param>
        private void Kinematics(float dt)
        {
            // Update new position and heading
            shape.Position += velocity * dt;
            shape.Rotation += angularVelocity * dt;

            // Decaying Velocities, if the user hasn't recently
            // pressed down the thrust or spin keys then reduce speed
            if (!hasThrust) velocity = velocity * decayRate;
            if (!hasSpin) angularVelocity = angularVelocity * angularDecayRate;
        }
        public void Shoot(Dictionary<string, Projectile> dictProjectiles)
        {
            // Impart the ships current position and velocity to projectile
            Projectile p = new Projectile(GetGunPosition(), velocity, shape.Rotation);
            string key = p.Id;
            dictProjectiles.Add(key, p);
            // Restart counter
            wantsToShoot = false;
            isShotCharged = false;
            shotCounter = 0;

        }
        /// <summary>
        /// A utility function for collision checks that
        /// returns all the coordinates of each vertex
        /// in absolute frame coordinates
        /// </summary>
        /// <returns></returns>
        public List<Vector2f> GetVertices()
        {
            List<Vector2f> points = new List<Vector2f> { };
            for(Byte i = 0; i < shape.GetPointCount(); i++)
            {
                // Found this on SFML-dev.org
                // Note: That GetPoint gives you only vertices from point of 
                // view of the shape (local), that's why you need transform
                points.Add(shape.Transform.TransformPoint(shape.GetPoint(i)));
            }
            return points;
        }
        private void ChargeShot()
        {
            if (shotCounter >= shotChargingTime) isShotCharged = true;
            else shotCounter++;
            wantsToShoot = false;
        }
        private Vector2f GetGunPosition()
        {
            // This is the tip of the isoceles triangle that is the ship
            // I'm honestly not quite sure why I have to jump through so many
            // hoops to get absolute coordinates... But here we are.
            return shape.Transform.TransformPoint(shape.GetPoint(3));
        }
        private Vector2f GetThrusterPostion()
        {
            Vector2f p1 = shape.Transform.TransformPoint(shape.GetPoint(1));
            Vector2f p2 = shape.Transform.TransformPoint(shape.GetPoint(2));
            Vector2f d = (p1 - p2) / 2;
            return p2 + d;
        }
        public bool IsShotCharged
        {
            get
            {
                return isShotCharged;
            }
        }

        public bool WantsToShoot
        {
            get
            {
                return wantsToShoot;
            }
        }
    }
}

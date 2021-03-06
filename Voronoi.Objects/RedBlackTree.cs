﻿using System;

namespace Voronoi.Objects
{
    public class RBTree
    {
        #region Properties

        public RBNode Root = null;

        #endregion

        /// <summary>
        /// Inserts a successor to a node.
        /// </summary>
        public void InsertSuccessor(RBNode objNode, RBNode objSuccessor)
        {
            // Initialise some variables.
            RBNode objParent = null;
            RBNode objGrandpa = null;
            RBNode objUncle = null;

            if (objNode != null)
            {
                objSuccessor.Previous = objNode;
                objSuccessor.Next = objNode.Next;

                if (objNode.Next != null)
                {
                    objNode.Next.Previous = objSuccessor;
                }

                objNode.Next = objSuccessor;

                if (objNode.Right != null)
                {
                    // I think the line below is the same as the following code. Has to be tested.
                    //this.GetFirst(objNode.Right).Left = objSuccessor;

                    objNode = objNode.Right;

                    while (objNode.Left != null)
                    {
                        objNode = objNode.Left;
                    }

                    objNode.Left = objSuccessor;
                }
                else
                {
                    objNode.Right = objSuccessor;
                }

                objParent = objNode;
            }
            else if (this.Root != null)
            {
                objNode = this.GetFirst(this.Root);

                objSuccessor.Previous = null;
                objSuccessor.Next = objNode;
                objNode.Previous = objSuccessor;
                objNode.Left = objSuccessor;
                objParent = objNode;
            }
            else
            {
                objSuccessor.Previous = objSuccessor.Next = null;
                this.Root = objSuccessor;
                objParent = null;
            }

            objSuccessor.Left = objSuccessor.Right = null;
            objSuccessor.Parent = objParent;
            objSuccessor.Red = true;

            objNode = objSuccessor;

            while (objParent != null && objParent.Red)
            {
                objGrandpa = objParent.Parent;

                if (objParent == objGrandpa.Left)
                {
                    objUncle = objGrandpa.Right;

                    if (objUncle != null && objUncle.Red)
                    {
                        objParent.Red = objUncle.Red = false;
                        objGrandpa.Red = true;
                        objNode = objGrandpa;
                    }
                    else
                    {
                        if (objNode == objParent.Right)
                        {
                            this.RotateLeft(objParent);
                            objNode = objParent;
                            objParent = objNode.Parent;
                        }

                        objParent.Red = false;
                        objGrandpa.Red = true;

                        this.RotateRight(objGrandpa);
                    }
                }
                else
                {
                    objUncle = objGrandpa.Left;

                    if (objUncle != null && objUncle.Red)
                    {
                        objParent.Red = objUncle.Red = false;
                        objGrandpa.Red = true;
                        objNode = objGrandpa;
                    }
                    else
                    {
                        if (objNode == objParent.Left)
                        {
                            this.RotateRight(objParent);
                            objNode = objParent;
                            objParent = objNode.Parent;
                        }

                        objParent.Red = false;
                        objGrandpa.Red = true;

                        this.RotateLeft(objGrandpa);
                    }
                }

                objParent = objNode.Parent;
            }

            this.Root.Red = false;
        }

        /// <summary>
        /// Get the first node by walking along the left hand node chain.
        /// </summary>
        private RBNode GetFirst(RBNode objNode)
        {
            while (objNode.Left != null)
            {
                objNode = objNode.Left;
            }

            return objNode;
        }

        /// <summary>
        /// Get the last node by walking along the right hand node chain.
        /// </summary>
        private RBNode GetLast(RBNode objNode)
        {
            while (objNode.Right != null)
            {
                objNode = objNode.Right;
            }

            return objNode;
        }

        /// <summary>
        /// Rotate node left.
        /// </summary>
        private void RotateLeft(RBNode objNode)
        {
            RBNode objRight = objNode.Right;
            RBNode objParent = objNode.Parent;

            if (objParent != null)
            {
                if (objParent.Left == objNode)
                {
                    objParent.Left = objRight;
                }
                else
                {
                    objParent.Right = objRight;
                }
            }
            else
            {
                this.Root = objRight;
            }

            objRight.Parent = objParent;
            objNode.Parent = objRight;
            objNode.Right = objRight.Left;

            if (objNode.Right != null)
            {
                objNode.Right.Parent = objNode;
            }

            objRight.Left = objNode;
        }

        /// <summary>
        /// Rotate node right.
        /// </summary>
        private void RotateRight(RBNode objNode)
        {
            RBNode objLeft = objNode.Left;
            RBNode objParent = objNode.Parent;

            if (objParent != null)
            {
                if (objParent.Left == objNode)
                {
                    objParent.Left = objLeft;
                }
                else
                {
                    objParent.Right = objLeft;
                }
            }
            else
            {
                this.Root = objLeft;
            }

            objLeft.Parent = objParent;
            objNode.Parent = objLeft;
            objNode.Left = objLeft.Right;

            if (objNode.Left != null)
            {
                objNode.Left.Parent = objNode;
            }

            objLeft.Right = objNode;
        }

        /// <summary>
        /// Removes a node from the tree.
        /// </summary>
        public void RemoveNode(RBNode objNode)
        {
            if (objNode.Next != null)
            {
                objNode.Next.Previous = objNode.Previous;
            }

            if (objNode.Previous != null)
            {
                objNode.Previous.Next = objNode.Next;
            }

            objNode.Next = objNode.Previous = null;

            RBNode objParent = objNode.Parent;
            RBNode objLeft = objNode.Left;
            RBNode objRight = objNode.Right;
            RBNode objNext = null;

            if (objLeft == null)
            {
                objNext = objRight;
            }
            else if (objRight == null)
            {
                objNext = objLeft;
            }
            else
            {
                objNext = this.GetFirst(objRight);
            }

            if (objParent != null)
            {
                if (objParent.Left == objNode)
                {
                    objParent.Left = objNext;
                }
                else
                {
                    objParent.Right = objNext;
                }
            }
            else
            {
                this.Root = objNext;
            }

            // Enforce red-black rules.
            bool blnIsRed;

            if (objLeft != null && objRight != null)
            {
                blnIsRed = objNext.Red;
                objNext.Red = objNode.Red;
                objNext.Left = objLeft;
                objLeft.Parent = objNext;

                if (objNext != objRight)
                {
                    objParent = objNext.Parent;
                    objNext.Parent = objNode.Parent;
                    objNode = objNext.Right;
                    objParent.Left = objNode;
                    objNext.Right = objRight;
                    objRight.Parent = objNext;
                }
                else
                {
                    objNext.Parent = objParent;
                    objParent = objNext;
                    objNode = objNext.Right;
                }
            }
            else
            {
                blnIsRed = objNode.Red;
                objNode = objNext;
            }

            // Node is now the sole successor's child and parent is new parent, since the successor can have been moved.
            if (objNode != null)
            {
                objNode.Parent = objParent;
            }

            // The easy cases.
            if (blnIsRed)
            {
                return;
            }

            if (objNode != null && objNode.Red)
            {
                objNode.Red = false;

                return;
            }

            // The other cases.
            RBNode objSibling = null;

            do
            {
                if (objNode == this.Root)
                {
                    break;
                }

                if (objNode == objParent.Left)
                {
                    objSibling = objParent.Right;

                    if (objSibling.Red)
                    {
                        objSibling.Red = false;
                        objParent.Red = true;
                        this.RotateLeft(objParent);
                        objSibling = objParent.Right;
                    }

                    if ((objSibling.Left != null && objSibling.Left.Red) || (objSibling.Right != null && objSibling.Right.Red))
                    {
                        if (objSibling.Right == null || !objSibling.Right.Red)
                        {
                            objSibling.Left.Red = false;
                            objSibling.Red = true;
                            this.RotateRight(objSibling);
                            objSibling = objParent.Right;
                        }

                        objSibling.Red = objParent.Red;
                        objParent.Red = objSibling.Right.Red = false;
                        this.RotateLeft(objParent);
                        objNode = this.Root;

                        break;
                    }
                }
                else
                {
                    objSibling = objParent.Left;

                    if (objSibling.Red)
                    {
                        objSibling.Red = false;
                        objParent.Red = true;
                        this.RotateRight(objParent);
                        objSibling = objParent.Left;
                    }

                    if ((objSibling.Left != null && objSibling.Left.Red) || (objSibling.Right != null && objSibling.Right.Red))
                    {
                        if (objSibling.Left == null || !objSibling.Left.Red)
                        {
                            objSibling.Right.Red = false;
                            objSibling.Red = true;
                            this.RotateLeft(objSibling);
                            objSibling = objParent.Left;
                        }

                        objSibling.Red = objParent.Red;
                        objParent.Red = objSibling.Left.Red = false;
                        this.RotateRight(objParent);
                        objNode = this.Root;

                        break;
                    }
                }

                objSibling.Red = true;
                objNode = objParent;
                objParent = objParent.Parent;

            } while (!objNode.Red);

            if (objNode != null)
            {
                objNode.Red = false;
            }
        }
    }
}

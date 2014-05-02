﻿namespace Zone.UmbracoMapper
 {
     public class RecursiveAttribute : System.Attribute
     {
         public bool Recursive { get; private set; }

         public RecursiveAttribute(bool recursive = true)
         {
             Recursive = recursive;
         }
     }
 }
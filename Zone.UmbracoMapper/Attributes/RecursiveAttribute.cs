﻿namespace Zone.UmbracoMapper
 {
     using System;

     public class RecursiveAttribute : Attribute
     {
         public bool Recursive { get; private set; }

         public RecursiveAttribute(bool recursive = true)
         {
             Recursive = recursive;
         }
     }
 }
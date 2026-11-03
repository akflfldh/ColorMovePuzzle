return System.Linq.Enumerable.SelectMany(System.AppDomain.CurrentDomain.GetAssemblies(),a=>{try{return a.GetTypes();}catch{return new System.Type[0];}})
    .Where(t=>t.Namespace=="UnityEngine.Android"||t==typeof(UnityEngine.Application))
    .SelectMany(t=>t.GetMembers(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static).Where(m=>m.Name.ToLowerInvariant().Contains("back")).Select(m=>t.FullName+"."+m.Name+" "+m.MemberType)).ToArray();

# Custom policies

This page covers the **v7 API**, which Fences ships in the `Paramore.Fences` package. If you are writing new code, prefer the v8 API and see [Extensibility](../extensibility/index.md) instead.

> [!NOTE]
> The articles linked below were written for Polly, which Fences forked from. They describe the same v7 extensibility model that Fences carries, so they still apply — with `Polly` read as `Paramore.Fences` throughout. Fences does not host its own copies, so the links go to Polly's site.

From v7.0 of the policy API it is possible to [create your own custom policies](https://www.thepollyproject.org/2019/02/13/introducing-custom-polly-policies-and-polly-contrib-custom-policies-part-i/) outside the library. These custom policies integrate with all the existing machinery: the `Policy.Handle<>()` syntax; `PolicyWrap`; all the execution-dispatch overloads.

For more info see our blog series:

+ [Part I: Introducing custom Polly policies and the Polly.Contrib](https://www.thepollyproject.org/2019/02/13/introducing-custom-polly-policies-and-polly-contrib-custom-policies-part-i/)
+ [Part II: Authoring a non-reactive custom policy](https://www.thepollyproject.org/2019/02/13/authoring-a-proactive-polly-policy-custom-policies-part-ii/) (a policy which acts on all executions)
+ [Part III: Authoring a reactive custom policy](https://www.thepollyproject.org/2019/02/13/authoring-a-reactive-polly-policy-custom-policies-part-iii-2/) (a policy which react to faults).
+ [Part IV: Custom policies for all execution types](https://www.thepollyproject.org/2019/02/13/custom-policies-for-all-execution-types-custom-policies-part-iv/): sync and async, generic and non-generic.

Polly-Contrib provides a [starter template for a custom policy](https://github.com/Polly-Contrib/Polly.Contrib.CustomPolicyTemplates). It targets Polly, so its package references need swapping for Fences' before use.

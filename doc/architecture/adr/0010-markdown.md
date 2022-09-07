# Markdown

* Status: Accepted
* Deciders: @duizer, @endk-awo, @PeterAGY, @MartinSchmidt, @C-Christiansen, @ckr123, @robertrosborg
* Date: 2022-09-06

---

## Context and Problem Statement
We need to be able to support rendering of Markdown to HTML in some of our endpoints

---

## Considered Options

* https://github.com/xoofx/markdig
* https://github.com/RickStrahl/Westwind.AspNetCore.Markdown

---

## Decision Outcome

https://github.com/xoofx/markdig

## Rationale
This seems to be the one repo with most recent work on and most stars.
Even though it is in version 0.x.x and Markdig states that it is under construction, the other repo (Westwind.AspNetCore.Markdown) are using it.
It also seems quite simple to use

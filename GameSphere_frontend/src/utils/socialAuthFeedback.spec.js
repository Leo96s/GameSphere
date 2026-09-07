import { describe, expect, it } from "vitest"
import { getLinkedProviderIds, getSocialAuthErrorFeedback } from "./socialAuthFeedback"

describe("getSocialAuthErrorFeedback", () => {
  it("returns null when the user closed the popup", () => {
    expect(getSocialAuthErrorFeedback({ code: "auth/popup-closed-by-user" }, "Login failed")).toBeNull()
    expect(getSocialAuthErrorFeedback({ code: "auth/cancelled-popup-request" }, "Login failed")).toBeNull()
  })

  it("explains an account-exists-with-different-credential conflict", () => {
    const feedback = getSocialAuthErrorFeedback(
      { code: "auth/account-exists-with-different-credential" },
      "GitHub login failed",
    )
    expect(feedback.title).toBe("GitHub login failed")
    expect(feedback.description).toMatch(/different sign-in method/i)
  })

  it("explains a credential-already-in-use conflict", () => {
    const feedback = getSocialAuthErrorFeedback({ code: "auth/credential-already-in-use" }, "Unable to connect")
    expect(feedback.title).toBe("Unable to connect")
    expect(feedback.description).toMatch(/already connected/i)
  })

  it("falls back to the generic title for unknown errors", () => {
    expect(getSocialAuthErrorFeedback({ code: "auth/network-request-failed" }, "Login failed")).toEqual({
      title: "Login failed",
    })
    expect(getSocialAuthErrorFeedback(undefined, "Login failed")).toEqual({ title: "Login failed" })
  })
})

describe("getLinkedProviderIds", () => {
  const firebaseUser = {
    email: "person@example.com",
    providerData: [{ providerId: "google.com" }, { providerId: "github.com" }],
  }

  it("returns the linked provider ids when the email matches (case-insensitively)", () => {
    expect(getLinkedProviderIds(firebaseUser, "Person@Example.com")).toEqual(["google.com", "github.com"])
  })

  it("returns an empty list when the email does not match", () => {
    expect(getLinkedProviderIds(firebaseUser, "someone-else@example.com")).toEqual([])
  })

  it("returns an empty list when there is no firebase user or expected email", () => {
    expect(getLinkedProviderIds(null, "person@example.com")).toEqual([])
    expect(getLinkedProviderIds(firebaseUser, undefined)).toEqual([])
  })
})

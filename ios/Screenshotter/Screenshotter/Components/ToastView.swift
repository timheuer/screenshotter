import SwiftUI

struct ToastView: View {
    let message: String
    let isSuccess: Bool
    
    var body: some View {
        HStack(spacing: 12) {
            Image(systemName: isSuccess ? "checkmark.circle.fill" : "xmark.circle.fill")
                .font(.title2)
                .foregroundColor(.white)
            
            Text(message)
                .font(.subheadline)
                .fontWeight(.medium)
                .foregroundColor(.white)
                .lineLimit(2)
            
            Spacer()
        }
        .padding(.horizontal, 16)
        .padding(.vertical, 14)
        .background(
            RoundedRectangle(cornerRadius: 12)
                .fill(isSuccess ? Color.green : Color.red)
                .shadow(color: Color.black.opacity(0.2), radius: 8, y: 4)
        )
        .padding(.horizontal, 16)
    }
}

// MARK: - Toast Modifier

struct ToastModifier: ViewModifier {
    @Binding var isPresented: Bool
    let message: String
    let isSuccess: Bool
    let duration: TimeInterval
    
    func body(content: Content) -> some View {
        ZStack {
            content
            
            if isPresented {
                VStack {
                    ToastView(message: message, isSuccess: isSuccess)
                        .transition(.asymmetric(
                            insertion: .move(edge: .top).combined(with: .opacity),
                            removal: .move(edge: .top).combined(with: .opacity)
                        ))
                    Spacer()
                }
                .padding(.top, 50)
                .animation(.spring(response: 0.3, dampingFraction: 0.7), value: isPresented)
                .onAppear {
                    DispatchQueue.main.asyncAfter(deadline: .now() + duration) {
                        withAnimation {
                            isPresented = false
                        }
                    }
                }
            }
        }
    }
}

extension View {
    func toast(isPresented: Binding<Bool>, message: String, isSuccess: Bool = true, duration: TimeInterval = 3.0) -> some View {
        modifier(ToastModifier(isPresented: isPresented, message: message, isSuccess: isSuccess, duration: duration))
    }
}

#Preview("Success Toast") {
    ToastView(message: "Screenshot saved successfully!", isSuccess: true)
        .padding()
}

#Preview("Error Toast") {
    ToastView(message: "Failed to capture screenshot", isSuccess: false)
        .padding()
}
